package main

import (
	"context"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"github.com/gorilla/mux"
	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/mayvqt/ArrSync/internal/handlers"
	"github.com/mayvqt/ArrSync/internal/services"
	"github.com/sirupsen/logrus"
)

// Build-time variables (overridden via -ldflags during release builds)
var (
	Version   = "dev"
	Commit    = ""
	BuildTime = ""
)

// recoveryMiddleware ensures handler panics don't bring down the process.
func recoveryMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if rec := recover(); rec != nil {
				logrus.WithField("panic", rec).Error("panic recovered in handler")
				http.Error(w, "internal server error", http.StatusInternalServerError)
			}
		}()
		next.ServeHTTP(w, r)
	})
}

// startSupervisedServer runs an HTTP server and restarts it on unexpected failures
// with exponential backoff. It returns when ctx is cancelled (initiates graceful shutdown).
func startSupervisedServer(ctx context.Context, addr string, handler http.Handler) error {
	// channel carrying the current server for shutdown coordination
	var currentSrv *http.Server

	backoff := time.Second
	const maxBackoff = 30 * time.Second

	for {
		select {
		case <-ctx.Done():
			// If context cancelled and server exists, shut it down
			if currentSrv != nil {
				shutdownCtx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
				defer cancel()
				if err := currentSrv.Shutdown(shutdownCtx); err != nil {
					logrus.WithError(err).Error("error shutting down server")
				}
			}
			return nil
		default:
		}

		srv := &http.Server{
			Addr:         addr,
			Handler:      handler,
			ReadTimeout:  15 * time.Second,
			WriteTimeout: 15 * time.Second,
			IdleTimeout:  60 * time.Second,
		}
		currentSrv = srv

		errCh := make(chan error, 1)
		go func() {
			logrus.WithField("addr", addr).Info("ArrSync server started")
			if err := srv.ListenAndServe(); err != nil && err != http.ErrServerClosed {
				errCh <- err
			} else {
				errCh <- nil
			}
		}()

		select {
		case <-ctx.Done():
			// shutdown will be handled at top of loop
			continue
		case err := <-errCh:
			if err == nil {
				// Normal shutdown (server closed) - return
				logrus.Info("server stopped")
				return nil
			}

			// Unexpected error - log and attempt restart with backoff
			logrus.WithError(err).Error("server crashed unexpectedly, will attempt restart")
			time.Sleep(backoff)
			backoff *= 2
			if backoff > maxBackoff {
				backoff = maxBackoff
			}
			// loop will recreate server and attempt restart
		}
	}
}

func main() {
	cfg, err := config.Load()
	if err != nil {
		logrus.WithError(err).Fatal("Failed to load configuration")
	}

	// Log effective configuration (avoid printing sensitive fields like API keys)
	logrus.WithFields(logrus.Fields{
		"port":             cfg.Server.Port,
		"overseer_url":     cfg.Overseer.URL,
		"overseer_timeout": cfg.Overseer.Timeout.String(),
		"overseer_retries": cfg.Overseer.MaxRetries,
		"dry_run":          cfg.Overseer.DryRun,
	}).Info("Configuration loaded")
	// Initialize service chain
	overseerService := services.NewOverseerService(cfg.Overseer)
	cleanupService := services.NewCleanupService(overseerService)
	handler := handlers.NewHandler(cleanupService)

	// Validate Overseer connection on startup
	logrus.Info("Validating Overseer connection...")
	if err := overseerService.HealthCheck(); err != nil {
		logrus.WithError(err).Warn("Overseer health check failed - will run in degraded mode")
	} else {
		logrus.Info("Overseer connection validated successfully")
	}

	// Configure routes
	router := mux.NewRouter()
	router.HandleFunc("/webhook/sonarr", handler.HandleSonarrWebhook).Methods("POST")
	router.HandleFunc("/webhook/radarr", handler.HandleRadarrWebhook).Methods("POST")
	router.HandleFunc("/health", handler.HandleHealth).Methods("GET")

	// Add recovery middleware so handler panics don't crash the server
	router.Use(recoveryMiddleware)

	// Supervise the HTTP server so transient Listen/Serve errors don't exit the process.
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// Start supervised server and wait for it on shutdown
	done := make(chan error, 1)
	go func() {
		done <- startSupervisedServer(ctx, ":"+cfg.Server.Port, router)
	}()

	// Graceful shutdown on interrupt
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logrus.Info("Shutting down...")
	cancel()

	// Wait for supervised server to finish its shutdown work. startSupervisedServer
	// performs a 30s Shutdown(ctx) when ctx is cancelled; wait slightly longer
	// here to allow it to complete but do not block indefinitely.
	select {
	case err := <-done:
		if err != nil {
			logrus.WithError(err).Error("supervised server exited with error")
		} else {
			logrus.Info("supervised server stopped cleanly")
		}
	case <-time.After(35 * time.Second):
		logrus.Warn("timeout waiting for supervised server to stop; exiting")
	}

	logrus.Info("ArrSync stopped")
}
