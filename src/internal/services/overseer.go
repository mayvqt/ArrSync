package services

import (
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/sirupsen/logrus"
)

type OverseerService struct {
	config config.OverseerConfig
	client *http.Client
}

func NewOverseerService(cfg config.OverseerConfig) *OverseerService {
	return &OverseerService{
		config: cfg,
		client: &http.Client{Timeout: 30 * time.Second},
	}
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
	mediaID, err := s.getMediaIDByTmdbID(tmdbID, mediaType)
	if err != nil {
		return fmt.Errorf("failed to get %s for TMDB ID %d: %w", mediaType, tmdbID, err)
	}

	if mediaID == 0 {
		logrus.WithFields(logrus.Fields{
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Info("No media found to remove")
		return nil
	}

	if err := s.deleteMedia(mediaID); err != nil {
		logrus.WithError(err).WithFields(logrus.Fields{
			"mediaId":   mediaID,
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Error("Failed to delete media")
		return err
	}

	logrus.WithFields(logrus.Fields{
		"mediaId":   mediaID,
		"tmdbId":    tmdbID,
		"mediaType": mediaType,
	}).Info("Successfully removed media from Overseer")

	return nil
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

	logrus.WithFields(logrus.Fields{
		"url":        url,
		"statusCode": resp.StatusCode,
	}).Debug("API response")

	// 404 means media not found in Overseer (not an error - may not have been requested)
	if resp.StatusCode == http.StatusNotFound {
		logrus.WithFields(logrus.Fields{
			"tmdbId":    tmdbID,
			"mediaType": mediaType,
		}).Info("Media not found in Overseer (may not have been requested)")
		return 0, nil
	}

	if resp.StatusCode == http.StatusOK {
		var rawResponse map[string]interface{}
		if err := json.NewDecoder(resp.Body).Decode(&rawResponse); err != nil {
			return 0, err
		}

		logrus.WithField("response", rawResponse).Debug("Full API response")

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
