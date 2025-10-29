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
			"tvdbId": webhook.Series.TvdbID,
		}).Info("Processing series deletion")

		// Overseer uses TMDB ID for all media types
		if webhook.Series.TmdbID > 0 {
			return s.overseer.RemoveRequestsByTmdbID(webhook.Series.TmdbID)
		}

		// Fallback to TVDB (limited support)
		if webhook.Series.TvdbID > 0 {
			return s.overseer.RemoveRequestsByTvdbID(webhook.Series.TvdbID)
		}

		logrus.WithField("title", webhook.Series.Title).Warn("No TMDB or TVDB ID available")
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

		if webhook.Movie.TmdbID > 0 {
			return s.overseer.RemoveRequestsByTmdbID(webhook.Movie.TmdbID)
		}

		logrus.WithField("title", webhook.Movie.Title).Warn("No TMDB ID available")
	}
	return nil
}
