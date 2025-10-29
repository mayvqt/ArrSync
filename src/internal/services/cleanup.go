package services

import (
	"github.com/mayvqt/ArrSync/internal/models"
	"github.com/sirupsen/logrus"
)

type CleanupService struct {
	overseer *OverseerService
}

func NewCleanupService(overseer *OverseerService) *CleanupService {
	return &CleanupService{
		overseer: overseer,
	}
}

// ProcessSonarrWebhook processes webhook events from Sonarr
func (s *CleanupService) ProcessSonarrWebhook(webhook models.SonarrWebhook) error {
	switch webhook.EventType {
	case "Test":
		logrus.Info("Received test webhook from Sonarr")
		return nil
	case "SeriesDelete":
		if webhook.Series != nil {
			logrus.WithFields(logrus.Fields{
				"seriesTitle": webhook.Series.Title,
				"tvdbId":      webhook.Series.TvdbID,
				"imdbId":      webhook.Series.ImdbID,
			}).Info("Processing series deletion from Sonarr")

			if webhook.Series.TvdbID > 0 {
				return s.overseer.RemoveRequestsByTvdbID(webhook.Series.TvdbID)
			}
			logrus.WithField("seriesTitle", webhook.Series.Title).Warn("Series has no TVDB ID, cannot remove from Overseer")
		}
	default:
		logrus.WithField("eventType", webhook.EventType).Debug("Ignoring Sonarr webhook event")
	}

	return nil
}

// ProcessRadarrWebhook processes webhook events from Radarr
func (s *CleanupService) ProcessRadarrWebhook(webhook models.RadarrWebhook) error {
	switch webhook.EventType {
	case "Test":
		logrus.Info("Received test webhook from Radarr")
		return nil
	case "MovieDelete":
		if webhook.Movie != nil {
			logrus.WithFields(logrus.Fields{
				"movieTitle": webhook.Movie.Title,
				"tmdbId":     webhook.Movie.TmdbID,
				"imdbId":     webhook.Movie.ImdbID,
			}).Info("Processing movie deletion from Radarr")

			if webhook.Movie.TmdbID > 0 {
				return s.overseer.RemoveRequestsByTmdbID(webhook.Movie.TmdbID)
			}
			logrus.WithField("movieTitle", webhook.Movie.Title).Warn("Movie has no TMDB ID, cannot remove from Overseer")
		}
	default:
		logrus.WithField("eventType", webhook.EventType).Debug("Ignoring Radarr webhook event")
	}

	return nil
}
