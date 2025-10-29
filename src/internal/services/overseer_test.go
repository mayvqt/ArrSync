package services

import (
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
)

func TestGetMediaID_MediaInfoID(t *testing.T) {
	// Mock Overseer endpoint returning mediaInfo.id
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		fmt.Fprint(w, `{"mediaInfo": {"id": 12345}}`)
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	svc := NewOverseerService(cfg)
	// inject client that talks to the test server
	svc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	id, err := svc.getMediaIDByTmdbID(999, "movie")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if id != 12345 {
		t.Fatalf("expected id 12345, got %d", id)
	}
}

func TestGetMediaID_TopLevelID(t *testing.T) {
	// Mock Overseer endpoint returning top-level id
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		fmt.Fprint(w, `{"id": 777}`)
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	svc := NewOverseerService(cfg)
	svc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	id, err := svc.getMediaIDByTmdbID(1000, "tv")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if id != 777 {
		t.Fatalf("expected id 777, got %d", id)
	}
}

func TestGetMediaID_NotFound(t *testing.T) {
	// Mock Overseer returning 404
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusNotFound)
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	svc := NewOverseerService(cfg)
	svc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	id, err := svc.getMediaIDByTmdbID(55, "movie")
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if id != 0 {
		t.Fatalf("expected id 0 for not found, got %d", id)
	}
}

func TestDeleteMedia_Success(t *testing.T) {
	// Mock DELETE endpoint
	called := false
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.Method == http.MethodDelete {
			called = true
			w.WriteHeader(http.StatusNoContent)
			return
		}
		w.WriteHeader(http.StatusNotFound)
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	svc := NewOverseerService(cfg)
	svc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	if err := svc.deleteMedia(42); err != nil {
		t.Fatalf("unexpected error deleting media: %v", err)
	}
	if !called {
		t.Fatalf("expected delete endpoint to be called")
	}
}

func TestRemoveMovieByTmdbID_FullFlow(t *testing.T) {
	// Test that RemoveMovieByTmdbID uses getMediaID and deleteMedia; simulate both
	step := 0
	h := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		switch step {
		case 0:
			// first request: lookup
			json.NewEncoder(w).Encode(map[string]interface{}{"mediaInfo": map[string]interface{}{"id": 9001}})
			step++
		case 1:
			// second request: delete
			if r.Method == http.MethodDelete {
				w.WriteHeader(http.StatusOK)
				return
			}
			w.WriteHeader(http.StatusBadRequest)
		}
	}))
	defer h.Close()

	cfg := config.OverseerConfig{URL: h.URL, APIKey: "test", Timeout: 2 * time.Second, MaxRetries: 1}
	svc := NewOverseerService(cfg)
	svc.SetHTTPClient(&http.Client{Timeout: cfg.Timeout})

	if err := svc.RemoveMovieByTmdbID(10); err != nil {
		t.Fatalf("RemoveMovieByTmdbID failed: %v", err)
	}
}
