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

func main() {
	// Load configuration
	cfg, err := config.Load()
	if err != nil {
		logrus.WithError(err).Fatal("Failed to load configuration")
	}

	// Initialize services
	overseerService := services.NewOverseerService(cfg.Overseer)
	cleanupService := services.NewCleanupService(overseerService)

	// Initialize handlers
	handler := handlers.NewHandler(cleanupService)

	// Setup router
	router := mux.NewRouter()
	router.HandleFunc("/webhook/sonarr", handler.HandleSonarrWebhook).Methods("POST")
	router.HandleFunc("/webhook/radarr", handler.HandleRadarrWebhook).Methods("POST")
	router.HandleFunc("/health", handler.HandleHealth).Methods("GET")

	// Setup server
	server := &http.Server{
		Addr:         ":" + cfg.Server.Port,
		Handler:      router,
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 15 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	// Start server in a goroutine
	go func() {
		logrus.WithField("port", cfg.Server.Port).Info("Starting arrsync server")
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logrus.WithError(err).Fatal("Failed to start server")
		}
	}()

	// Wait for interrupt signal to gracefully shutdown
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logrus.Info("Shutting down arrsync server...")

	// Create a deadline for shutdown
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	// Attempt graceful shutdown
	if err := server.Shutdown(ctx); err != nil {
		logrus.WithError(err).Fatal("Server forced to shutdown")
	}

	logrus.Info("ArrSync server exited")
}
