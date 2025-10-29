package services

import (
	"encoding/json"
	"fmt"
	"net/http"

	"github.com/sirupsen/logrus"
)

// getMediaIDByTmdbID looks up Overseer media ID using TMDB ID
// Tries movie endpoint first, then TV show endpoint
func (s *OverseerService) getMediaIDByTmdbID(tmdbID int) (int, error) {
	// Try both movie and TV endpoints
	endpoints := []struct {
		path      string
		mediaType string
	}{
		{fmt.Sprintf("/api/v1/movie/%d", tmdbID), "movie"},
		{fmt.Sprintf("/api/v1/tv/%d", tmdbID), "tv"},
	}

	for _, endpoint := range endpoints {
		mediaID, err := s.queryMediaEndpoint(endpoint.path, endpoint.mediaType, tmdbID)
		if err != nil {
			return 0, err
		}
		if mediaID > 0 {
			return mediaID, nil
		}
	}

	logrus.WithField("tmdbId", tmdbID).Debug("No media found")
	return 0, nil
}

// queryMediaEndpoint queries a specific Overseer API endpoint for media
func (s *OverseerService) queryMediaEndpoint(path, mediaType string, tmdbID int) (int, error) {
	url := s.config.URL + path

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
			"mediaType": mediaType,
			"tmdbId":    tmdbID,
		}).Debug("Found media")

		return media.ID, nil
	}

	return 0, nil
}
