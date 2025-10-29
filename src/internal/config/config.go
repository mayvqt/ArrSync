package config

import (
	"fmt"
	"os"

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
	URL    string
	APIKey string
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
			URL:    getEnv("OVERSEER_URL", ""),
			APIKey: getEnv("OVERSEER_API_KEY", ""),
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
