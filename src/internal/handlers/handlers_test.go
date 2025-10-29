package handlers

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/mayvqt/ArrSync/internal/models"
	"github.com/mayvqt/ArrSync/internal/services"
)

func TestHandleRadarrWebhook_Success(t *testing.T) {
	// Setup overseer test server that responds to lookup and delete
	step := 0
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		if r.Method == http.MethodGet {
			if err := json.NewEncoder(w).Encode(map[string]interface{}{"mediaInfo": map[string]interface{}{"id": 101}}); err != nil {
				t.Fatalf("failed to encode mediaInfo: %v", err)
			}
			return
		}
		if r.Method == http.MethodDelete {
			w.WriteHeader(http.StatusNoContent)
			step++
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer srv.Close()

	cfg := config.OverseerConfig{URL: srv.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	oSvc := services.NewOverseerService(cfg)
	oSvc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	cleanup := services.NewCleanupService(oSvc)
	h := NewHandler(cleanup)

	payload := models.RadarrWebhook{EventType: "MovieDelete", Movie: &models.RadarrMovie{TmdbID: 11}}
	body, _ := json.Marshal(payload)

	req := httptest.NewRequest("POST", "/radarr", bytes.NewReader(body))
	w := httptest.NewRecorder()
	h.HandleRadarrWebhook(w, req)

	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Result().StatusCode)
	}
	if step != 1 {
		t.Fatalf("expected delete step to run once, got %d", step)
	}
}

func TestHandleSonarrWebhook_Success(t *testing.T) {
	// Setup overseer server that will respond with not found (no delete)
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusNotFound)
	}))
	defer srv.Close()

	cfg := config.OverseerConfig{URL: srv.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	oSvc := services.NewOverseerService(cfg)
	oSvc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	cleanup := services.NewCleanupService(oSvc)
	h := NewHandler(cleanup)

	payload := models.SonarrWebhook{EventType: "SeriesDelete", Series: &models.SonarrSeries{TmdbID: 22}}
	body, _ := json.Marshal(payload)

	req := httptest.NewRequest("POST", "/sonarr", bytes.NewReader(body))
	w := httptest.NewRecorder()
	h.HandleSonarrWebhook(w, req)

	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200, got %d", w.Result().StatusCode)
	}
}
