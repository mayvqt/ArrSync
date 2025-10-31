# ArrSync

[![CI](https://github.com/mayvqt/ArrSync/actions/workflows/ci.yml/badge.svg)](https://github.com/mayvqt/ArrSync/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/mayvqt/ArrSync?style=flat-square)](https://github.com/mayvqt/ArrSync/releases/latest)
[![Go](https://img.shields.io/badge/Go-1.21+-00ADD8?style=flat&logo=go)](https://golang.org/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Overview

ArrSync is a lightweight service designed to automatically synchronize media deletions from Sonarr and Radarr to Overseerr. When media is removed from your Sonarr or Radarr library, ArrSync receives the deletion webhook and automatically removes the corresponding entry from Overseerr, allowing users to re-request that content if desired.

The service is built for reliability and 24/7 operation with minimal supervision, featuring automatic recovery from transient failures, graceful degradation when external services are unavailable, and comprehensive error handling to prevent crashes.

## Features

### Core Functionality

**Webhook Integration**
- Receives and processes Sonarr series deletion webhooks
- Receives and processes Radarr movie deletion webhooks
- Automatic TMDB ID extraction and matching
- Test webhook support for validation

**Overseerr Integration**
- Automatic media removal from Overseerr when deletions occur
- TMDB-based media lookup with support for both movies and TV shows
- Graceful handling of media not found in Overseerr
- Background health monitoring with configurable polling interval

### Reliability & Resilience

**Supervised Server Architecture**
- HTTP server runs under supervision with automatic restart on unexpected failures
- Exponential backoff with jitter for restart attempts (max 30 seconds)
- Recovery middleware catches handler panics and returns HTTP 500 instead of crashing
- Graceful shutdown with configurable timeout (30 seconds default)
- Context-based cancellation for clean shutdown coordination

**API Resilience**
- Exponential backoff retry logic for Overseerr API calls (configurable max retries)
- Jitter added to retry delays to prevent thundering herd
- HTTP connection pooling with keep-alive for efficiency
- Request timeouts to prevent hanging connections
- Automatic availability tracking with graceful degradation when Overseerr is down

**Health Monitoring**
- Background Overseerr health polling with configurable interval
- Real-time availability state tracking
- Health check endpoint reporting service and Overseerr status
- Degraded mode operation when Overseerr is temporarily unavailable

### Configuration & Deployment

**Flexible Configuration**
- Environment variable-based configuration with sensible defaults
- Optional .env file support for local development
- Required fields validated at startup with clear error messages
- Log level configuration (debug, info, warn, error)
- Build-time metadata embedding (version, commit, build time)

**Operational Features**
- Dry-run mode for safe testing without performing deletions
- Structured logging with contextual fields
- Cross-platform support (Windows, Linux, macOS)
- Single static binary with no external dependencies
- Minimal resource footprint

## Installation

### Download Pre-built Binary

Download the appropriate binary for your platform from the [latest release](https://github.com/mayvqt/ArrSync/releases/latest):

- `arrsync-windows-amd64.exe` - Windows (x64)
- `arrsync-windows-arm64.exe` - Windows (ARM)
- `arrsync-linux-amd64` - Linux (x64)
- `arrsync-linux-arm64` - Linux (ARM)
- `arrsync-darwin-amd64` - macOS (Intel)
- `arrsync-darwin-arm64` - macOS (Apple Silicon)

All releases include SHA256 checksums in `checksums.txt` for verification.

Example download and verification (Linux):

```bash
curl -LO https://github.com/mayvqt/ArrSync/releases/latest/download/arrsync-linux-amd64
curl -LO https://github.com/mayvqt/ArrSync/releases/latest/download/checksums.txt
sha256sum -c checksums.txt 2>&1 | grep arrsync-linux-amd64
chmod +x arrsync-linux-amd64
```

### Build from Source

Requirements: Go 1.21 or later

```bash
git clone https://github.com/mayvqt/ArrSync.git
cd ArrSync/src
go build -o arrsync .
```

To embed version metadata during build:

```bash
VERSION="v1.0.0"
COMMIT=$(git rev-parse --short HEAD)
BUILD_TIME=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

go build -ldflags "-X main.Version=${VERSION} -X main.Commit=${COMMIT} -X main.BuildTime=${BUILD_TIME}" -o arrsync .
```

## Configuration

ArrSync is configured entirely through environment variables. You can set these directly in your environment or use a `.env` file in the working directory.

### Required Configuration

| Variable | Description |
|----------|-------------|
| `OVERSEER_URL` | Base URL of your Overseerr instance (e.g., `http://overseer:5055`) |
| `OVERSEER_API_KEY` | Overseerr API key (found in Settings → General → API Key) |

### Optional Configuration

| Variable | Description | Default |
|----------|-------------|---------|
| `PORT` | HTTP server listen port | `8080` |
| `API_TIMEOUT` | Timeout for Overseerr API requests (seconds) | `30` |
| `MAX_RETRIES` | Maximum retry attempts for failed Overseerr API calls | `3` |
| `DRY_RUN` | When true, logs deletion actions without performing them | `false` |
| `LOG_LEVEL` | Logging verbosity (debug, info, warn, error) | `info` |
| `OVERSEER_POLL_INTERVAL` | Background health check interval (seconds); set to 0 to disable | `60` |

### Example .env File

```env
# Required
OVERSEER_URL=http://192.168.1.100:5055
OVERSEER_API_KEY=your-overseerr-api-key-here

# Optional - customize as needed
PORT=8080
API_TIMEOUT=30
MAX_RETRIES=3
DRY_RUN=false
LOG_LEVEL=info
OVERSEER_POLL_INTERVAL=60
```

## Usage

### Running the Service

Start ArrSync after configuring your environment:

```bash
./arrsync
```

On startup, you should see output similar to:

```
INFO[0000] Configuration loaded                          dry_run=false overseer_retries=3 overseer_timeout=30s overseer_url="http://overseer:5055" port=8080
INFO[0000] Validating Overseer connection...
INFO[0000] Overseer connection validated successfully
INFO[0000] ArrSync server started                        addr=":8080"
```

### Webhook Configuration

#### Sonarr

1. Navigate to Settings → Connect → Add → Webhook
2. Configure the webhook:
   - **Name**: ArrSync
   - **On Series Delete**: Checked
   - **URL**: `http://your-arrsync-host:8080/webhook/sonarr`
   - **Method**: POST
3. Test the connection and save

#### Radarr

1. Navigate to Settings → Connect → Add → Webhook
2. Configure the webhook:
   - **Name**: ArrSync
   - **On Movie Delete**: Checked
   - **URL**: `http://your-arrsync-host:8080/webhook/radarr`
   - **Method**: POST
3. Test the connection and save

### API Endpoints

#### POST /webhook/sonarr

Receives Sonarr deletion webhooks. Expects JSON payload with `eventType` and `series` fields containing TMDB ID.

#### POST /webhook/radarr

Receives Radarr deletion webhooks. Expects JSON payload with `eventType` and `movie` fields containing TMDB ID.

#### GET /health

Returns service health status and Overseerr availability.

Response (healthy):
```json
{
  "status": "healthy",
  "service": "arrsync",
  "healthy": true,
  "overseer": "available"
}
```

Response (degraded - Overseerr unavailable):
```json
{
  "status": "degraded",
  "service": "arrsync",
  "healthy": false,
  "overseer": "unavailable"
}
```

## Running as a Service

For production deployments, run ArrSync under a process manager to ensure it restarts automatically.

### systemd (Linux)

Create `/etc/systemd/system/arrsync.service`:

```ini
[Unit]
Description=ArrSync Service
After=network.target

[Service]
Type=simple
User=arrsync
Group=arrsync
ExecStart=/usr/local/bin/arrsync
Restart=always
RestartSec=5
StartLimitBurst=5
StartLimitIntervalSec=60

# Environment variables
Environment=PORT=8080
Environment=OVERSEER_URL=http://overseer:5055
Environment=OVERSEER_API_KEY=your_api_key_here
Environment=OVERSEER_POLL_INTERVAL=60

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable arrsync
sudo systemctl start arrsync
sudo systemctl status arrsync
```

View logs:

```bash
sudo journalctl -u arrsync -f
```

### Windows Service (NSSM)

Download NSSM from https://nssm.cc/ and install the service:

```powershell
nssm install ArrSync "C:\path\to\arrsync.exe"
nssm set ArrSync AppDirectory "C:\path\to"
nssm set ArrSync AppStdout "C:\path\to\logs\arrsync.log"
nssm set ArrSync AppStderr "C:\path\to\logs\arrsync-error.log"
nssm set ArrSync AppEnvironmentExtra PORT=8080
nssm set ArrSync AppEnvironmentExtra OVERSEER_URL=http://overseer:5055
nssm set ArrSync AppEnvironmentExtra OVERSEER_API_KEY=your_api_key_here
nssm set ArrSync AppEnvironmentExtra OVERSEER_POLL_INTERVAL=60
nssm start ArrSync
```

### Docker

Example Dockerfile:

```dockerfile
FROM golang:1.21-alpine AS builder
WORKDIR /app
COPY src/ .
RUN go mod download && \
    go build -o arrsync .

FROM alpine:latest
RUN apk --no-cache add ca-certificates
WORKDIR /app
COPY --from=builder /app/arrsync .
EXPOSE 8080
CMD ["./arrsync"]
```

Example docker-compose.yml:

```yaml
version: '3.8'
services:
  arrsync:
    build: .
    container_name: arrsync
    ports:
      - "8080:8080"
    environment:
      OVERSEER_URL: http://overseer:5055
      OVERSEER_API_KEY: ${OVERSEER_API_KEY}
      OVERSEER_POLL_INTERVAL: 60
      LOG_LEVEL: info
    restart: unless-stopped
```

## Testing

### Dry-Run Mode

Test ArrSync without performing actual deletions:

```bash
DRY_RUN=true ./arrsync
```

When enabled, deletion operations are logged but not executed:

```
WARN[0000] DRY RUN MODE ENABLED - No deletions will be performed
WARN[0042] [DRY RUN] Would delete media from Overseer   mediaId=1234 mediaType=tv tmdbId=98765
```

### Manual Testing

Test webhook endpoints:

```bash
# Test Sonarr webhook
curl -X POST http://localhost:8080/webhook/sonarr \
  -H "Content-Type: application/json" \
  -d '{"eventType": "Test"}'

# Test Radarr webhook
curl -X POST http://localhost:8080/webhook/radarr \
  -H "Content-Type: application/json" \
  -d '{"eventType": "Test"}'

# Check health
curl http://localhost:8080/health
```

### Unit Tests

Run the test suite:

```bash
cd src
go test ./... -v
```

Run tests with coverage:

```bash
go test ./... -v -race -coverprofile=coverage.out -covermode=atomic
go tool cover -html=coverage.out
```

## Troubleshooting

### Overseer Connection Failed

If ArrSync cannot connect to Overseerr on startup:

1. Verify `OVERSEER_URL` is correct and accessible from the ArrSync host
2. Test connectivity: `curl http://your-overseer:5055/api/v1/status`
3. Verify the API key is correct in Overseerr Settings → General → API Key
4. Check firewall rules and network connectivity

ArrSync will continue running in degraded mode and retry connections during the background health check interval.

### Webhooks Not Working

If deletions are not being synchronized:

1. Verify ArrSync is reachable from Sonarr/Radarr: `curl http://arrsync:8080/health`
2. Test the webhook connection in Sonarr/Radarr settings
3. Check ArrSync logs for incoming webhook requests
4. Enable debug logging: `LOG_LEVEL=debug ./arrsync`
5. Verify the webhook URL uses the correct hostname and port

### Media Not Deleted from Overseerr

If media remains in Overseerr after deletion:

1. Check if `DRY_RUN=true` is enabled (should be `false` for production)
2. Verify the media exists in Overseerr before deletion
3. Check ArrSync logs for TMDB ID matching
4. Verify the deleted media had a valid TMDB ID in Sonarr/Radarr
5. Check Overseerr logs for API errors

### High Memory or CPU Usage

ArrSync is designed to be lightweight. If you observe high resource usage:

1. Check `OVERSEER_POLL_INTERVAL` - very short intervals may cause unnecessary API calls
2. Verify `MAX_RETRIES` is set to a reasonable value (3-5)
3. Review logs for error loops or excessive retry attempts
4. Consider adjusting `API_TIMEOUT` if requests are hanging

## Development

### Project Structure

```
ArrSync/
├── src/
│   ├── main.go                         # Entry point, supervised server, signal handling
│   ├── main_test.go                    # Main package tests (recovery, shutdown)
│   ├── go.mod                          # Go dependencies
│   ├── go.sum                          # Dependency checksums
│   └── internal/
│       ├── config/
│       │   ├── config.go               # Configuration loading and validation
│       │   └── config_test.go          # Config tests
│       ├── handlers/
│       │   ├── webhook.go              # HTTP request handlers
│       │   └── handlers_test.go        # Handler tests
│       ├── models/
│       │   ├── overseer.go             # Overseerr API models
│       │   └── webhooks.go             # Webhook payload models
│       └── services/
│           ├── overseer.go             # Overseerr API client with retry logic
│           ├── overseer_test.go        # Overseerr service tests
│           ├── monitor_test.go         # Health monitor tests
│           ├── cleanup.go              # Media deletion orchestration
│           └── cleanup_test.go         # Cleanup service tests
├── .env.example                        # Example configuration
├── .github/
│   └── workflows/
│       ├── ci.yml                      # CI workflow (tests, lint, vuln check)
│       └── release.yml                 # Release workflow (builds, assets)
└── README.md
```

### CI/CD

The project uses GitHub Actions for continuous integration and releases.

**CI Workflow** (runs on all pushes and PRs):
- Go vet static analysis
- golangci-lint checks
- govulncheck security scanning
- Unit tests with race detector
- Coverage reporting

**Release Workflow** (runs on version tags):
- Runs full test suite
- Cross-compiles binaries for all platforms
- Generates SHA256 checksums
- Creates GitHub release with assets
- Generates build provenance attestation

To create a new release:

```bash
git tag -a v1.2.3 -m "Release v1.2.3"
git push origin v1.2.3
```

### Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Write tests for new functionality
4. Ensure all tests pass: `go test ./... -v`
5. Run linters: `golangci-lint run`
6. Commit with clear messages
7. Push to your fork and open a pull request

Pull requests will automatically run CI checks. Please ensure:
- All tests pass
- Code coverage does not decrease
- golangci-lint produces no warnings
- govulncheck finds no vulnerabilities

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/mayvqt/ArrSync/issues)
- **Discussions**: Ask questions or share ideas in [GitHub Discussions](https://github.com/mayvqt/ArrSync/discussions)

## Acknowledgments

Built for the *arr stack community to simplify media management workflows.
