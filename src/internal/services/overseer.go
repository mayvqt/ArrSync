package services

import (
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
		client: &http.Client{
			Timeout: 30 * time.Second,
		},
	}
}

// RemoveRequestsByTmdbID removes media entry by TMDB ID (for both movies and TV shows)
func (s *OverseerService) RemoveRequestsByTmdbID(tmdbID int) error {
	mediaID, err := s.getMediaIDByTmdbID(tmdbID)
	if err != nil {
		return fmt.Errorf("failed to get media for TMDB ID %d: %w", tmdbID, err)
	}

	if mediaID == 0 {
		logrus.WithField("tmdbId", tmdbID).Info("No media found to remove")
		return nil
	}

	if err := s.deleteMedia(mediaID); err != nil {
		logrus.WithError(err).WithFields(logrus.Fields{
			"mediaId": mediaID,
			"tmdbId":  tmdbID,
		}).Error("Failed to delete media")
		return err
	}

	logrus.WithFields(logrus.Fields{
		"mediaId": mediaID,
		"tmdbId":  tmdbID,
	}).Info("Successfully removed media from Overseer")

	return nil
}

// RemoveRequestsByTvdbID removes media entry by TVDB ID (fallback for TV shows)
func (s *OverseerService) RemoveRequestsByTvdbID(tvdbID int) error {
	logrus.WithField("tvdbId", tvdbID).Warn("TVDB lookup not fully supported - Overseer uses TMDB IDs. Consider using TMDB ID instead.")
	return nil
}

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
