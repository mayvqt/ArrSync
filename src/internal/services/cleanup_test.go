package services

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/mayvqt/ArrSync/internal/models"
)

func TestProcessRadarrWebhook_RemovesMovie(t *testing.T) {
	// Prepare a test server that handles lookup and delete
	step := 0
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		// lookup GET /api/v1/movie/{tmdb}
		if r.Method == http.MethodGet {
			if err := json.NewEncoder(w).Encode(map[string]interface{}{"mediaInfo": map[string]interface{}{"id": 4242}}); err != nil {
				t.Fatalf("failed to encode mediaInfo: %v", err)
			}
			return
		}
		// DELETE /api/v1/media/{id}
		if r.Method == http.MethodDelete {
			w.WriteHeader(http.StatusNoContent)
			step++
			return
		}
		w.WriteHeader(http.StatusBadRequest)
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	oSvc := NewOverseerService(cfg)
	oSvc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	cleanup := NewCleanupService(oSvc)

	webhook := models.RadarrWebhook{
		EventType: "MovieDelete",
		Movie: &models.RadarrMovie{
			TmdbID: 123,
		},
	}

	if err := cleanup.ProcessRadarrWebhook(webhook); err != nil {
		t.Fatalf("ProcessRadarrWebhook failed: %v", err)
	}
	if step != 1 {
		t.Fatalf("expected delete step to run once, got %d", step)
	}
}

func TestProcessSonarrWebhook_NoTMDB(t *testing.T) {
	// When no TMDB ID present, should not attempt deletion and return nil
	cfg := config.OverseerConfig{URL: "http://example.invalid", APIKey: "test", Timeout: 1 * time.Second, MaxRetries: 0}
	oSvc := NewOverseerService(cfg)
	cleanup := NewCleanupService(oSvc)

	webhook := models.SonarrWebhook{
		EventType: "SeriesDelete",
		Series: &models.SonarrSeries{
			Title:  "NoTMDBShow",
			TmdbID: 0,
		},
	}

	if err := cleanup.ProcessSonarrWebhook(webhook); err != nil {
		t.Fatalf("ProcessSonarrWebhook should not error when no TMDB ID: %v", err)
	}
}
