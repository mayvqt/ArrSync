package models

import "time"

// SonarrWebhook represents the webhook payload from Sonarr
type SonarrWebhook struct {
	EventType        string              `json:"eventType"`
	InstanceName     string              `json:"instanceName"`
	ApplicationURL   string              `json:"applicationUrl"`
	Series           *SonarrSeries       `json:"series,omitempty"`
	Episodes         []SonarrEpisode     `json:"episodes,omitempty"`
	EpisodeFile      *SonarrEpisodeFile  `json:"episodeFile,omitempty"`
	IsUpgrade        bool                `json:"isUpgrade"`
	DeletedFiles     interface{}         `json:"deletedFiles,omitempty"`
	CustomFormatInfo *SonarrCustomFormat `json:"customFormatInfo,omitempty"`
}

type SonarrSeries struct {
	ID                int           `json:"id"`
	Title             string        `json:"title"`
	TitleSlug         string        `json:"titleSlug"`
	Path              string        `json:"path"`
	TvdbID            int           `json:"tvdbId"`
	TvMazeID          int           `json:"tvMazeId"`
	TmdbID            int           `json:"tmdbId"`
	ImdbID            string        `json:"imdbId"`
	SeriesType        string        `json:"seriesType"`
	Network           string        `json:"network"`
	AirTime           string        `json:"airTime"`
	Status            string        `json:"status"`
	Overview          string        `json:"overview"`
	PosterURL         string        `json:"posterUrl"`
	BannerURL         string        `json:"bannerUrl"`
	Added             time.Time     `json:"added"`
	Genres            []string      `json:"genres"`
	Tags              []interface{} `json:"tags"`
	Year              int           `json:"year"`
	QualityProfileID  int           `json:"qualityProfileId"`
	LanguageProfileID int           `json:"languageProfileId"`
	Runtime           int           `json:"runtime"`
	Images            []SonarrImage `json:"images"`
}

type SonarrEpisode struct {
	ID                    int       `json:"id"`
	EpisodeNumber         int       `json:"episodeNumber"`
	SeasonNumber          int       `json:"seasonNumber"`
	Title                 string    `json:"title"`
	AirDate               string    `json:"airDate"`
	AirDateUTC            time.Time `json:"airDateUtc"`
	Overview              string    `json:"overview"`
	HasFile               bool      `json:"hasFile"`
	Monitored             bool      `json:"monitored"`
	AbsoluteEpisodeNumber int       `json:"absoluteEpisodeNumber"`
	TvdbID                int       `json:"tvDbId"`
}

type SonarrEpisodeFile struct {
	ID            int                  `json:"id"`
	RelativePath  string               `json:"relativePath"`
	Path          string               `json:"path"`
	Size          int64                `json:"size"`
	DateAdded     time.Time            `json:"dateAdded"`
	ReleaseGroup  string               `json:"releaseGroup"`
	Quality       SonarrQuality        `json:"quality"`
	CustomFormats []SonarrCustomFormat `json:"customFormats"`
}

type SonarrQuality struct {
	ID         int    `json:"id"`
	Name       string `json:"name"`
	Resolution int    `json:"resolution"`
}

type SonarrCustomFormat struct {
	ID   int    `json:"id"`
	Name string `json:"name"`
}

type SonarrDeletedFile struct {
	ID           int    `json:"id"`
	RelativePath string `json:"relativePath"`
	Path         string `json:"path"`
}

type SonarrImage struct {
	CoverType string `json:"coverType"`
	URL       string `json:"url"`
}

// RadarrWebhook represents the webhook payload from Radarr
type RadarrWebhook struct {
	EventType        string              `json:"eventType"`
	InstanceName     string              `json:"instanceName"`
	ApplicationURL   string              `json:"applicationUrl"`
	Movie            *RadarrMovie        `json:"movie,omitempty"`
	MovieFile        *RadarrMovieFile    `json:"movieFile,omitempty"`
	IsUpgrade        bool                `json:"isUpgrade"`
	DeletedFiles     interface{}         `json:"deletedFiles,omitempty"`
	CustomFormatInfo *RadarrCustomFormat `json:"customFormatInfo,omitempty"`
}

type RadarrMovie struct {
	ID                    int              `json:"id"`
	Title                 string           `json:"title"`
	TitleSlug             string           `json:"titleSlug"`
	OriginalTitle         string           `json:"originalTitle"`
	AlternativeTitles     []RadarrAltTitle `json:"alternativeTitles"`
	SecondaryYearSourceID int              `json:"secondaryYearSourceId"`
	SortTitle             string           `json:"sortTitle"`
	SizeOnDisk            int64            `json:"sizeOnDisk"`
	Status                string           `json:"status"`
	Overview              string           `json:"overview"`
	InCinemas             time.Time        `json:"inCinemas"`
	PhysicalRelease       time.Time        `json:"physicalRelease"`
	DigitalRelease        time.Time        `json:"digitalRelease"`
	Images                []RadarrImage    `json:"images"`
	Website               string           `json:"website"`
	Year                  int              `json:"year"`
	YouTubeTrailerID      string           `json:"youTubeTrailerId"`
	Studio                string           `json:"studio"`
	Path                  string           `json:"path"`
	QualityProfileID      int              `json:"qualityProfileId"`
	HasFile               bool             `json:"hasFile"`
	MovieFileID           int              `json:"movieFileId"`
	Monitored             bool             `json:"monitored"`
	MinimumAvailability   string           `json:"minimumAvailability"`
	IsAvailable           bool             `json:"isAvailable"`
	FolderName            string           `json:"folderName"`
	Runtime               int              `json:"runtime"`
	CleanTitle            string           `json:"cleanTitle"`
	ImdbID                string           `json:"imdbId"`
	TmdbID                int              `json:"tmdbId"`
	Genres                []string         `json:"genres"`
	Tags                  []interface{}    `json:"tags"`
	Added                 time.Time        `json:"added"`
	AddOptions            RadarrAddOptions `json:"addOptions"`
}

type RadarrAltTitle struct {
	SourceType string `json:"sourceType"`
	MovieID    int    `json:"movieId"`
	Title      string `json:"title"`
	SourceID   int    `json:"sourceId"`
	Votes      int    `json:"votes"`
	VoteCount  int    `json:"voteCount"`
	Language   string `json:"language"`
}

type RadarrImage struct {
	CoverType string `json:"coverType"`
	URL       string `json:"url"`
}

type RadarrAddOptions struct {
	IgnoreEpisodesWithFiles    bool `json:"ignoreEpisodesWithFiles"`
	IgnoreEpisodesWithoutFiles bool `json:"ignoreEpisodesWithoutFiles"`
	SearchForMovie             bool `json:"searchForMovie"`
}

type RadarrMovieFile struct {
	ID            int                  `json:"id"`
	RelativePath  string               `json:"relativePath"`
	Path          string               `json:"path"`
	Size          int64                `json:"size"`
	DateAdded     time.Time            `json:"dateAdded"`
	ReleaseGroup  string               `json:"releaseGroup"`
	Quality       RadarrQuality        `json:"quality"`
	CustomFormats []RadarrCustomFormat `json:"customFormats"`
	Edition       string               `json:"edition"`
}

type RadarrQuality struct {
	ID         int    `json:"id"`
	Name       string `json:"name"`
	Resolution int    `json:"resolution"`
}

type RadarrCustomFormat struct {
	ID   int    `json:"id"`
	Name string `json:"name"`
}

type RadarrDeletedFile struct {
	ID           int    `json:"id"`
	RelativePath string `json:"relativePath"`
	Path         string `json:"path"`
}

// OverseerRequest represents a request in Overseer
type OverseerRequest struct {
	ID        int           `json:"id"`
	Status    int           `json:"status"`
	Media     OverseerMedia `json:"media"`
	Type      string        `json:"type"`
	CreatedAt time.Time     `json:"createdAt"`
	UpdatedAt time.Time     `json:"updatedAt"`
}

type OverseerMedia struct {
	ID        int       `json:"id"`
	MediaType string    `json:"mediaType"`
	TmdbID    int       `json:"tmdbId"`
	TvdbID    int       `json:"tvdbId,omitempty"`
	ImdbID    string    `json:"imdbId,omitempty"`
	Status    int       `json:"status"`
	CreatedAt time.Time `json:"createdAt"`
	UpdatedAt time.Time `json:"updatedAt"`
}

// OverseerRequestsResponse represents the response from Overseer's requests API
type OverseerRequestsResponse struct {
	PageInfo OverseerPageInfo  `json:"pageInfo"`
	Results  []OverseerRequest `json:"results"`
}
