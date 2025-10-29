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

	if resp.StatusCode == http.StatusOK {
		var media struct {
			ID int `json:"id"`
		}
		if err := json.NewDecoder(resp.Body).Decode(&media); err != nil {
			return 0, err
		}

		logrus.WithFields(logrus.Fields{
			"mediaId":   media.ID,
			"mediaType": mediaType,
			"tmdbId":    tmdbID,
		}).Debug("Found media")

		return media.ID, nil
	}

	return 0, nil
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
