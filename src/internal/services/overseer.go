package services

import (
	"encoding/json"
	"fmt"
	"net/http"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
	"github.com/mayvqt/ArrSync/internal/models"
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

// RemoveRequestsByTmdbID removes all requests for a movie by TMDB ID
func (s *OverseerService) RemoveRequestsByTmdbID(tmdbID int) error {
	requests, err := s.getRequestsByTmdbID(tmdbID)
	if err != nil {
		return fmt.Errorf("failed to get requests for TMDB ID %d: %w", tmdbID, err)
	}

	if len(requests) == 0 {
		logrus.WithField("tmdbId", tmdbID).Info("No requests found to remove")
		return nil
	}

	for _, request := range requests {
		if err := s.deleteRequest(request.ID); err != nil {
			logrus.WithError(err).WithFields(logrus.Fields{
				"requestId": request.ID,
				"tmdbId":    tmdbID,
			}).Error("Failed to delete request")
			continue
		}
		logrus.WithFields(logrus.Fields{
			"requestId": request.ID,
			"tmdbId":    tmdbID,
		}).Info("Successfully removed request from Overseer")
	}

	return nil
}

// RemoveRequestsByTvdbID removes all requests for a TV series by TVDB ID
func (s *OverseerService) RemoveRequestsByTvdbID(tvdbID int) error {
	requests, err := s.getRequestsByTvdbID(tvdbID)
	if err != nil {
		return fmt.Errorf("failed to get requests for TVDB ID %d: %w", tvdbID, err)
	}

	if len(requests) == 0 {
		logrus.WithField("tvdbId", tvdbID).Info("No requests found to remove")
		return nil
	}

	for _, request := range requests {
		if err := s.deleteRequest(request.ID); err != nil {
			logrus.WithError(err).WithFields(logrus.Fields{
				"requestId": request.ID,
				"tvdbId":    tvdbID,
			}).Error("Failed to delete request")
			continue
		}
		logrus.WithFields(logrus.Fields{
			"requestId": request.ID,
			"tvdbId":    tvdbID,
		}).Info("Successfully removed request from Overseer")
	}

	return nil
}

func (s *OverseerService) getRequestsByTmdbID(tmdbID int) ([]models.OverseerRequest, error) {
	url := fmt.Sprintf("%s/api/v1/request", s.config.URL)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return nil, err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := s.client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	var response models.OverseerRequestsResponse
	if err := json.NewDecoder(resp.Body).Decode(&response); err != nil {
		return nil, err
	}

	logrus.WithFields(logrus.Fields{
		"totalRequests": len(response.Results),
		"searchTmdbId":  tmdbID,
	}).Debug("Searching Overseer requests for matching TMDB ID")

	var matchingRequests []models.OverseerRequest
	for _, request := range response.Results {
		logrus.WithFields(logrus.Fields{
			"requestId":     request.ID,
			"mediaType":     request.Media.MediaType,
			"mediaTmdbId":   request.Media.TmdbID,
			"mediaTvdbId":   request.Media.TvdbID,
			"mediaStatus":   request.Media.Status,
			"requestStatus": request.Status,
		}).Debug("Checking request")

		if request.Media.TmdbID == tmdbID && request.Media.MediaType == "movie" {
			matchingRequests = append(matchingRequests, request)
		}
	}

	logrus.WithFields(logrus.Fields{
		"matchingRequests": len(matchingRequests),
		"tmdbId":           tmdbID,
	}).Debug("Found matching requests")

	return matchingRequests, nil
}

func (s *OverseerService) getRequestsByTvdbID(tvdbID int) ([]models.OverseerRequest, error) {
	url := fmt.Sprintf("%s/api/v1/request", s.config.URL)

	req, err := http.NewRequest("GET", url, nil)
	if err != nil {
		return nil, err
	}

	req.Header.Set("X-API-Key", s.config.APIKey)
	req.Header.Set("Content-Type", "application/json")

	resp, err := s.client.Do(req)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return nil, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	var response models.OverseerRequestsResponse
	if err := json.NewDecoder(resp.Body).Decode(&response); err != nil {
		return nil, err
	}

	logrus.WithFields(logrus.Fields{
		"totalRequests": len(response.Results),
		"searchTvdbId":  tvdbID,
	}).Debug("Searching Overseer requests for matching TVDB ID")

	var matchingRequests []models.OverseerRequest
	for _, request := range response.Results {
		logrus.WithFields(logrus.Fields{
			"requestId":     request.ID,
			"mediaType":     request.Media.MediaType,
			"mediaTvdbId":   request.Media.TvdbID,
			"mediaTmdbId":   request.Media.TmdbID,
			"mediaStatus":   request.Media.Status,
			"requestStatus": request.Status,
		}).Debug("Checking request")

		if request.Media.TvdbID == tvdbID && request.Media.MediaType == "tv" {
			matchingRequests = append(matchingRequests, request)
		}
	}

	logrus.WithFields(logrus.Fields{
		"matchingRequests": len(matchingRequests),
		"tvdbId":           tvdbID,
	}).Debug("Found matching requests")

	return matchingRequests, nil
}

func (s *OverseerService) deleteRequest(requestID int) error {
	url := fmt.Sprintf("%s/api/v1/request/%d", s.config.URL, requestID)

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
