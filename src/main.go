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
	cfg, err := config.Load()
	if err != nil {
		logrus.WithError(err).Fatal("Failed to load configuration")
	}

	// Initialize service chain
	overseerService := services.NewOverseerService(cfg.Overseer)
	cleanupService := services.NewCleanupService(overseerService)
	handler := handlers.NewHandler(cleanupService)

	// Configure routes
	router := mux.NewRouter()
	router.HandleFunc("/webhook/sonarr", handler.HandleSonarrWebhook).Methods("POST")
	router.HandleFunc("/webhook/radarr", handler.HandleRadarrWebhook).Methods("POST")
	router.HandleFunc("/health", handler.HandleHealth).Methods("GET")

	// Start HTTP server
	server := &http.Server{
		Addr:         ":" + cfg.Server.Port,
		Handler:      router,
		ReadTimeout:  15 * time.Second,
		WriteTimeout: 15 * time.Second,
		IdleTimeout:  60 * time.Second,
	}

	go func() {
		logrus.WithField("port", cfg.Server.Port).Info("ArrSync server started")
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			logrus.WithError(err).Fatal("Server failed")
		}
	}()

	// Graceful shutdown on interrupt
	quit := make(chan os.Signal, 1)
	signal.Notify(quit, syscall.SIGINT, syscall.SIGTERM)
	<-quit

	logrus.Info("Shutting down...")

	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()

	if err := server.Shutdown(ctx); err != nil {
		logrus.WithError(err).Fatal("Forced shutdown")
	}

	logrus.Info("ArrSync stopped")
}
