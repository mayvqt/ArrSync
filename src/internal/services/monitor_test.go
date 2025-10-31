package services

import (
	"context"
	"testing"
	"time"

	"github.com/mayvqt/ArrSync/internal/config"
)

// Ensure Monitor returns promptly when context is cancelled before the first tick.
func TestMonitorCancelsQuickly(t *testing.T) {
	s := NewOverseerService(config.OverseerConfig{
		URL:        "",
		APIKey:     "",
		Timeout:    1 * time.Second,
		MaxRetries: 0,
	})

	ctx, cancel := context.WithCancel(context.Background())
	cancel()

	done := make(chan struct{})
	go func() {
		s.Monitor(ctx, 50*time.Millisecond)
		close(done)
	}()

	select {
	case <-done:
		// ok
	case <-time.After(2 * time.Second):
		t.Fatalf("Monitor did not return after context cancel")
	}
}
