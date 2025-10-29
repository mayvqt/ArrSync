package services

import (
	"encoding/json"
	"fmt"
	"net"
	"net/http"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/sirupsen/logrus"
)

type OverseerService struct {
	config    config.OverseerConfig
	client    *http.Client
	available bool
}

func NewOverseerService(cfg config.OverseerConfig) *OverseerService {
	// Configure HTTP client with connection pooling
	transport := &http.Transport{
		MaxIdleConns:        100,
		MaxIdleConnsPerHost: 10,
		IdleConnTimeout:     90 * time.Second,
		DialContext: (&net.Dialer{
			Timeout:   10 * time.Second,
			KeepAlive: 30 * time.Second,
		}).DialContext,
	}

	return &OverseerService{
		config: cfg,
		client: &http.Client{
			Timeout:   cfg.Timeout,
			Transport: transport,
		},
		available: true,
	}
}

// HealthCheck verifies Overseer API is reachable
func (s *OverseerService) HealthCheck() error {
	url := fmt.Sprintf("%s/api/v1/status", s.config.URL)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)

	resp, err := s.client.Do(req)
	if err != nil {
		s.available = false
		return fmt.Errorf("overseer unreachable: %w", err)
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		s.available = false
		return fmt.Errorf("overseer health check failed: status %d", resp.StatusCode)
	}

	s.available = true
	return nil
}

// IsAvailable returns whether Overseer is currently reachable
func (s *OverseerService) IsAvailable() bool {
	return s.available
}

// RemoveMovieByTmdbID removes a movie from Overseer by TMDB ID
func (s *OverseerService) RemoveMovieByTmdbID(tmdbID int) error {
	return s.removeMediaByTmdbID(tmdbID, "movie")
}

// RemoveTVShowByTmdbID removes a TV show from Overseer by TMDB ID
func (s *OverseerService) RemoveTVShowByTmdbID(tmdbID int) error {
	return s.removeMediaByTmdbID(tmdbID, "tv")
}

// removeMediaByTmdbID looks up and deletes media by TMDB ID and type
func (s *OverseerService) removeMediaByTmdbID(tmdbID int, mediaType string) error {
	if !s.available {
		logrus.Warn("Overseer unavailable, skipping deletion")
		return nil
	}

	mediaID, err := s.getMediaIDByTmdbIDWithRetry(tmdbID, mediaType)
	if err != nil {
		s.available = false
		return fmt.Errorf("failed to get %s for TMDB ID %d: %w", mediaType, tmdbID, err)
	}

	if mediaID == 0 {
		logrus.WithFields(logrus.Fields{
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Info("No media found to remove")
		return nil
	}

	if s.config.DryRun {
		logrus.WithFields(logrus.Fields{
			"mediaId":   mediaID,
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Warn("[DRY RUN] Would delete media from Overseer")
		return nil
	}

	if err := s.deleteMediaWithRetry(mediaID); err != nil {
		s.available = false
		logrus.WithError(err).WithFields(logrus.Fields{
			"mediaId":   mediaID,
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Error("Failed to delete media")
		return err
	}

	s.available = true
	logrus.WithFields(logrus.Fields{
		"mediaId":   mediaID,
		"tmdbId":    tmdbID,
		"mediaType": mediaType,
	}).Info("Successfully removed media from Overseer")

	return nil
}

// retry executes a function with exponential backoff
func (s *OverseerService) retry(operation string, fn func() error) error {
	var err error
	for attempt := 0; attempt <= s.config.MaxRetries; attempt++ {
		err = fn()
		if err == nil {
			return nil
		}

		if attempt < s.config.MaxRetries {
			backoff := time.Duration(1<<uint(attempt)) * time.Second
			logrus.WithFields(logrus.Fields{
				"operation": operation,
				"attempt":   attempt + 1,
				"backoff":   backoff,
				"error":     err,
			}).Warn("Retrying after error")
			time.Sleep(backoff)
		}
	}
	return fmt.Errorf("max retries exceeded: %w", err)
}

// getMediaIDByTmdbIDWithRetry wraps getMediaIDByTmdbID with retry logic
func (s *OverseerService) getMediaIDByTmdbIDWithRetry(tmdbID int, mediaType string) (int, error) {
	var mediaID int
	err := s.retry("getMediaID", func() error {
		id, err := s.getMediaIDByTmdbID(tmdbID, mediaType)
		if err != nil {
			return err
		}
		mediaID = id
		return nil
	})
	return mediaID, err
}

// deleteMediaWithRetry wraps deleteMedia with retry logic
func (s *OverseerService) deleteMediaWithRetry(mediaID int) error {
	return s.retry("deleteMedia", func() error {
		return s.deleteMedia(mediaID)
	})
}

// getMediaIDByTmdbID looks up Overseer media ID using TMDB ID and media type
func (s *OverseerService) getMediaIDByTmdbID(tmdbID int, mediaType string) (int, error) {
	url := fmt.Sprintf("%s/api/v1/%s/%d", s.config.URL, mediaType, tmdbID)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return 0, err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := s.client.Do(req)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	// 404 means media not found in Overseer (not an error - may not have been requested)
	if resp.StatusCode == http.StatusNotFound {
		return 0, nil
	}

	if resp.StatusCode == http.StatusOK {
		var rawResponse map[string]interface{}
		if err := json.NewDecoder(resp.Body).Decode(&rawResponse); err != nil {
			return 0, err
		}

		// Try to extract media ID from mediaInfo
		if mediaInfo, ok := rawResponse["mediaInfo"].(map[string]interface{}); ok {
			if id, ok := mediaInfo["id"].(float64); ok {
				return int(id), nil
			}
		}

		// Fallback to top-level ID
		if id, ok := rawResponse["id"].(float64); ok {
			return int(id), nil
		}

		return 0, fmt.Errorf("could not find media ID in response")
	}

	return 0, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
}

// deleteMedia removes media entry from Overseer (cascades to requests, seasons, episodes)
func (s *OverseerService) deleteMedia(mediaID int) error {
	url := fmt.Sprintf("%s/api/v1/media/%d", s.config.URL, mediaID)

	req, err := http.NewRequest("DELETE", url, nil)
	if err != nil {
		return err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)

	resp, err := s.client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK && resp.StatusCode != http.StatusNoContent {
		return fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	return nil
}
