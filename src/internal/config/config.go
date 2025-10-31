package config

import (
	"fmt"
	"os"
	"strconv"
	"time"

	"github.com/joho/godotenv"
	"github.com/sirupsen/logrus"
)

type Config struct {
	Server   ServerConfig
	Overseer OverseerConfig
	Log      LogConfig
}

type ServerConfig struct {
	Port string
}

type OverseerConfig struct {
	URL        string
	APIKey     string
	Timeout    time.Duration
	MaxRetries int
	DryRun     bool
	// HealthInterval controls how often the app polls Overseer for health.
	// Value is parsed from seconds in the environment variable OVERSEER_POLL_INTERVAL.
	HealthInterval time.Duration
}

type LogConfig struct {
	Level string
}

func Load() (*Config, error) {
	// Load .env file if it exists
	if err := godotenv.Load(); err != nil {
		logrus.Debug("No .env file found, using environment variables")
	}

	config := &Config{
		Server: ServerConfig{
			Port: getEnv("PORT", "8080"),
		},
		Overseer: OverseerConfig{
			URL:            getEnv("OVERSEER_URL", ""),
			APIKey:         getEnv("OVERSEER_API_KEY", ""),
			Timeout:        getEnvDuration("API_TIMEOUT", 30*time.Second),
			MaxRetries:     getEnvInt("MAX_RETRIES", 3),
			DryRun:         getEnvBool("DRY_RUN", false),
			HealthInterval: getEnvDuration("OVERSEER_POLL_INTERVAL", 60*time.Second),
		},
		Log: LogConfig{
			Level: getEnv("LOG_LEVEL", "info"),
		},
	}

	// Validate required configuration
	if err := config.validate(); err != nil {
		return nil, err
	}

	// Set log level
	if err := config.setLogLevel(); err != nil {
		return nil, err
	}

	if config.Overseer.DryRun {
		logrus.Warn("DRY RUN MODE ENABLED - No deletions will be performed")
	}

	return config, nil
}

func (c *Config) validate() error {
	if c.Overseer.URL == "" {
		return fmt.Errorf("OVERSEER_URL is required")
	}
	if c.Overseer.APIKey == "" {
		return fmt.Errorf("OVERSEER_API_KEY is required")
	}
	return nil
}

func (c *Config) setLogLevel() error {
	level, err := logrus.ParseLevel(c.Log.Level)
	if err != nil {
		return fmt.Errorf("invalid log level: %s", c.Log.Level)
	}
	logrus.SetLevel(level)
	return nil
}

func getEnv(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

func getEnvInt(key string, defaultValue int) int {
	if value := os.Getenv(key); value != "" {
		if i, err := strconv.Atoi(value); err == nil {
			return i
		}
	}
	return defaultValue
}

func getEnvBool(key string, defaultValue bool) bool {
	if value := os.Getenv(key); value != "" {
		if b, err := strconv.ParseBool(value); err == nil {
			return b
		}
	}
	return defaultValue
}

func getEnvDuration(key string, defaultValue time.Duration) time.Duration {
	if value := os.Getenv(key); value != "" {
		if i, err := strconv.Atoi(value); err == nil {
			return time.Duration(i) * time.Second
		}
	}
	return defaultValue
}
