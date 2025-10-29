# ArrSync - Overseer Cleanup Service

A lightweight Go service that automatically removes items from Overseer when they are deleted from Sonarr or Radarr, allowing them to be requested again.

## Features

- 🔄 **Automatic Cleanup**: Removes deleted items from Overseer automatically
- 📺 **Sonarr Integration**: Handles TV series deletions via webhooks
- 🎬 **Radarr Integration**: Handles movie deletions via webhooks
- 🚀 **Lightweight**: Single binary with minimal resource usage
- 🔧 **Cross Platform**: Runs on Windows, Linux, and macOS
- 📊 **Health Monitoring**: Built-in health check endpoint
- 🔒 **Secure**: Environment-based configuration for sensitive data

## Quick Start

### 1. Configuration

Copy the example environment file and configure it:

```bash
cp .env.example .env
```

Edit `.env` with your settings:

```env
# Server Configuration
PORT=8080

# Overseer Configuration
OVERSEER_URL=http://localhost:5055
OVERSEER_API_KEY=your-overseer-api-key-here

# Logging Configuration
LOG_LEVEL=info
```

### 2. Build and Run

```bash
# Navigate to src directory
cd src

# Install dependencies
go mod tidy

# Build the application
go build -o arrsync

# Run the service
./arrsync
```

Or run directly:

```bash
cd src
go run .
```

### 3. Configure Webhooks

#### Sonarr Webhook Configuration

1. Go to Sonarr → Settings → Connect
2. Add a new Webhook connection
3. Configure:
   - **Name**: ArrSync
   - **URL**: `http://your-server:8080/webhook/sonarr`
   - **Method**: POST
   - **On Series Delete**: ✅ Enabled

#### Radarr Webhook Configuration

1. Go to Radarr → Settings → Connect
2. Add a new Webhook connection
3. Configure:
   - **Name**: ArrSync
   - **URL**: `http://your-server:8080/webhook/radarr`
   - **Method**: POST
   - **On Movie Delete**: ✅ Enabled

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/webhook/sonarr` | POST | Receives webhook events from Sonarr |
| `/webhook/radarr` | POST | Receives webhook events from Radarr |
| `/health` | GET | Health check endpoint |

## Configuration

### Environment Variables

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `PORT` | Server port | No | `8080` |
| `OVERSEER_URL` | Overseer base URL | Yes | - |
| `OVERSEER_API_KEY` | Overseer API key | Yes | - |
| `LOG_LEVEL` | Logging level (debug, info, warn, error) | No | `info` |

### Getting Overseer API Key

1. Log into Overseer as an admin
2. Go to Settings → General
3. Copy the API Key from the API section

## Docker Deployment

### Using Docker Compose

Create a `docker-compose.yml`:

```yaml
version: '3.8'
services:
  arrsync:
    build: .
    ports:
      - "8080:8080"
    environment:
      - OVERSEER_URL=http://overseer:5055
      - OVERSEER_API_KEY=your-overseer-api-key
      - LOG_LEVEL=info
    restart: unless-stopped
```

### Dockerfile

```dockerfile
FROM golang:1.21-alpine AS builder

WORKDIR /app
COPY src/go.mod src/go.sum ./
RUN go mod download

COPY src/ .
RUN go build -o arrsync .

FROM alpine:latest
RUN apk --no-cache add ca-certificates tzdata
WORKDIR /root/

COPY --from=builder /app/arrsync .

EXPOSE 8080
CMD ["./arrsync"]
```

## Development

### Project Structure

```
├── src/
│   ├── main.go              # Application entry point
│   ├── go.mod               # Go module definition
│   ├── go.sum               # Go dependencies
│   └── internal/
│       ├── config/          # Configuration management
│       ├── handlers/        # HTTP handlers
│       ├── models/          # Data structures
│       └── services/        # Business logic
├── .env.example             # Example environment file
├── .gitignore
└── README.md
```

### Building

```bash
# Build for current platform (from src directory)
cd src
go build -o arrsync

# Build for Linux
GOOS=linux GOARCH=amd64 go build -o arrsync-linux

# Build for Windows
GOOS=windows GOARCH=amd64 go build -o arrsync.exe
```

### Testing

Test the webhooks using curl:

```bash
# Test Sonarr webhook
curl -X POST http://localhost:8080/webhook/sonarr \
  -H "Content-Type: application/json" \
  -d '{"eventType": "Test"}'

# Test Radarr webhook
curl -X POST http://localhost:8080/webhook/radarr \
  -H "Content-Type: application/json" \
  -d '{"eventType": "Test"}'

# Health check
curl http://localhost:8080/health
```

## Troubleshooting

### Common Issues

1. **Connection refused to Overseer**
   - Verify `OVERSEER_URL` is correct and accessible
   - Check if Overseer is running and accepting connections

2. **Authentication errors**
   - Verify `OVERSEER_API_KEY` is correct
   - Ensure the API key has sufficient permissions

3. **Webhooks not triggering**
   - Check Sonarr/Radarr webhook configuration
   - Verify the service URL is accessible from Sonarr/Radarr
   - Check logs for error messages

### Logging

Set `LOG_LEVEL=debug` for detailed logging:

```bash
LOG_LEVEL=debug ./arrsync
```

## License

MIT License - see LICENSE file for details.

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request
