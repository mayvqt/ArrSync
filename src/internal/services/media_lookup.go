package services

import (
	"encoding/json"
	"fmt"
	"net/http"

	"github.com/sirupsen/logrus"
)

func (s *OverseerService) getMediaIDByTmdbID(tmdbID int) (int, error) {
	// Try movie first
	url := fmt.Sprintf("%s/api/v1/movie/%d", s.config.URL, tmdbID)

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
			ID        int    `json:"id"`
			MediaType string `json:"mediaType"`
		}
		if err := json.NewDecoder(resp.Body).Decode(&media); err != nil {
			return 0, err
		}
		logrus.WithFields(logrus.Fields{
			"mediaId":   media.ID,
			"mediaType": media.MediaType,
			"tmdbId":    tmdbID,
		}).Debug("Found media by TMDB ID (movie)")
		return media.ID, nil
	}

	// Try TV show
	url = fmt.Sprintf("%s/api/v1/tv/%d", s.config.URL, tmdbID)

	req, err = http.NewRequest("GET", url, nil)
	if err != nil {
		return 0, err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err = s.client.Do(req)
	if err != nil {
		return 0, err
	}
	defer resp.Body.Close()

	if resp.StatusCode == http.StatusOK {
		var media struct {
			ID        int    `json:"id"`
			MediaType string `json:"mediaType"`
		}
		if err := json.NewDecoder(resp.Body).Decode(&media); err != nil {
			return 0, err
		}
		logrus.WithFields(logrus.Fields{
			"mediaId":   media.ID,
			"mediaType": media.MediaType,
			"tmdbId":    tmdbID,
		}).Debug("Found media by TMDB ID (tv)")
		return media.ID, nil
	}

	logrus.WithField("tmdbId", tmdbID).Debug("No media found with this TMDB ID")
	return 0, nil
}
