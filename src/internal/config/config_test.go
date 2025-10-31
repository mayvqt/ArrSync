package config

import (
	"os"
	"testing"
	"time"
)

func TestLoadFromEnv(t *testing.T) {
	// Set environment variables that Load requires
	os.Setenv("PORT", "12345")
	os.Setenv("OVERSEER_URL", "https://overseer.example")
	os.Setenv("OVERSEER_API_KEY", "fakekey")
	os.Setenv("API_TIMEOUT", "5")
	os.Setenv("MAX_RETRIES", "2")
	os.Setenv("DRY_RUN", "true")
	os.Setenv("LOG_LEVEL", "debug")

	// Clean up
	defer func() {
		os.Unsetenv("PORT")
		os.Unsetenv("OVERSEER_URL")
		os.Unsetenv("OVERSEER_API_KEY")
		os.Unsetenv("API_TIMEOUT")
		os.Unsetenv("MAX_RETRIES")
		os.Unsetenv("DRY_RUN")
		os.Unsetenv("LOG_LEVEL")
	}()

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load() returned error: %v", err)
	}

	if cfg.Server.Port != "12345" {
		t.Fatalf("expected port 12345, got %s", cfg.Server.Port)
	}
	if cfg.Overseer.URL != "https://overseer.example" {
		t.Fatalf("unexpected overseer url: %s", cfg.Overseer.URL)
	}
	if cfg.Overseer.APIKey != "fakekey" {
		t.Fatalf("unexpected api key")
	}
	if cfg.Overseer.Timeout < 4*time.Second || cfg.Overseer.Timeout > 6*time.Second {
		t.Fatalf("unexpected timeout: %v", cfg.Overseer.Timeout)
	}
	if cfg.Overseer.MaxRetries != 2 {
		t.Fatalf("unexpected max retries: %d", cfg.Overseer.MaxRetries)
	}
	if cfg.Overseer.DryRun != true {
		t.Fatalf("expected dry run true")
	}
}
