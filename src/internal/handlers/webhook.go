package handlers

import (
	"encoding/json"
	"net/http"

	"github.com/mayvqt/ArrSync/internal/models"
	"github.com/mayvqt/ArrSync/internal/services"
	"github.com/sirupsen/logrus"
)

type Handler struct {
	cleanup *services.CleanupService
}

func NewHandler(cleanup *services.CleanupService) *Handler {
	return &Handler{cleanup: cleanup}
}

// HandleSonarrWebhook processes incoming Sonarr webhooks
func (h *Handler) HandleSonarrWebhook(w http.ResponseWriter, r *http.Request) {
	var webhook models.SonarrWebhook

	if err := json.NewDecoder(r.Body).Decode(&webhook); err != nil {
		logrus.WithError(err).Error("Failed to decode Sonarr webhook")
		http.Error(w, "Invalid JSON", http.StatusBadRequest)
		return
	}

	logrus.WithFields(logrus.Fields{
		"eventType": webhook.EventType,
		"instance":  webhook.InstanceName,
	}).Info("Received Sonarr webhook")

	if err := h.cleanup.ProcessSonarrWebhook(webhook); err != nil {
		logrus.WithError(err).Error("Failed to process Sonarr webhook")
		http.Error(w, "Processing failed", http.StatusInternalServerError)
		return
	}

	h.sendSuccess(w)
}

// HandleRadarrWebhook processes incoming Radarr webhooks
func (h *Handler) HandleRadarrWebhook(w http.ResponseWriter, r *http.Request) {
	var webhook models.RadarrWebhook

	if err := json.NewDecoder(r.Body).Decode(&webhook); err != nil {
		logrus.WithError(err).Error("Failed to decode Radarr webhook")
		http.Error(w, "Invalid JSON", http.StatusBadRequest)
		return
	}

	logrus.WithFields(logrus.Fields{
		"eventType": webhook.EventType,
		"instance":  webhook.InstanceName,
	}).Info("Received Radarr webhook")

	if err := h.cleanup.ProcessRadarrWebhook(webhook); err != nil {
		logrus.WithError(err).Error("Failed to process Radarr webhook")
		http.Error(w, "Processing failed", http.StatusInternalServerError)
		return
	}

	h.sendSuccess(w)
}

// HandleHealth returns service health status
func (h *Handler) HandleHealth(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{
		"status":  "healthy",
		"service": "arrsync",
	})
}

// sendSuccess sends a standardized success response
func (h *Handler) sendSuccess(w http.ResponseWriter) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusOK)
	json.NewEncoder(w).Encode(map[string]string{"status": "success"})
}
