package services

import (
	"github.com/mayvqt/ArrSync/internal/models"
	"github.com/sirupsen/logrus"
)

type CleanupService struct {
	overseer *OverseerService
}

func NewCleanupService(overseer *OverseerService) *CleanupService {
	return &CleanupService{overseer: overseer}
}

// GetOverseerService returns the underlying Overseer service
func (s *CleanupService) GetOverseerService() *OverseerService {
	return s.overseer
}

// ProcessSonarrWebhook handles Sonarr webhook events and removes deleted series from Overseer
func (s *CleanupService) ProcessSonarrWebhook(webhook models.SonarrWebhook) error {
	switch webhook.EventType {
	case "Test":
		logrus.Info("Received test webhook from Sonarr")
		return nil
	case "SeriesDelete":
		if webhook.Series == nil {
			return nil
		}

		logrus.WithFields(logrus.Fields{
			"title":  webhook.Series.Title,
			"tmdbId": webhook.Series.TmdbID,
		}).Info("Processing series deletion")

		// Sonarr = TV shows, use TV endpoint directly
		if webhook.Series.TmdbID > 0 {
			return s.overseer.RemoveTVShowByTmdbID(webhook.Series.TmdbID)
		}

		logrus.WithField("title", webhook.Series.Title).Warn("No TMDB ID available")
	}
	return nil
}

// ProcessRadarrWebhook handles Radarr webhook events and removes deleted movies from Overseer
func (s *CleanupService) ProcessRadarrWebhook(webhook models.RadarrWebhook) error {
	switch webhook.EventType {
	case "Test":
		logrus.Info("Received test webhook from Radarr")
		return nil
	case "MovieDelete":
		if webhook.Movie == nil {
			return nil
		}

		logrus.WithFields(logrus.Fields{
			"title":  webhook.Movie.Title,
			"tmdbId": webhook.Movie.TmdbID,
		}).Info("Processing movie deletion")

		// Radarr = Movies, use movie endpoint directly
		if webhook.Movie.TmdbID > 0 {
			return s.overseer.RemoveMovieByTmdbID(webhook.Movie.TmdbID)
		}

		logrus.WithField("title", webhook.Movie.Title).Warn("No TMDB ID available")
	}
	return nil
}
