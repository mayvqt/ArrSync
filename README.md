# ArrSync

**Automatically sync deletions from Sonarr/Radarr to Overseer** - allowing media to be re-requested after deletion.

[![Go](https://img.shields.io/badge/Go-1.21+-00ADD8?style=flat&logo=go)](https://golang.org/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

## 🚀 Features

- ✅ **Automatic Sync** - Removes deleted items from Overseer automatically
- 📺 **Sonarr Integration** - Handles TV series deletions via webhooks  
- 🎬 **Radarr Integration** - Handles movie deletions via webhooks
- � **Retry Logic** - Exponential backoff for API failures
- 🏥 **Health Monitoring** - Built-in health checks with Overseer status
- 🧪 **Dry-Run Mode** - Test without actually deleting anything
- ⚡ **Connection Pooling** - Optimized HTTP performance
- 🛡️ **Graceful Degradation** - Continues running even if Overseer is temporarily down
- 🪶 **Lightweight** - Single binary, minimal resource usage
- 🌐 **Cross-Platform** - Windows, Linux, macOS

---

## 📋 Quick Start

### 1️⃣ Download or Build

**Option A: Download Binary**

Download the pre-built binaries from the GitHub release: https://github.com/mayvqt/ArrSync/releases/tag/v1.0.0

Available assets in the release:

- `arrsync-windows-amd64.exe` — Windows (x64)
- `arrsync-windows-arm64.exe` — Windows (ARM)
- `arrsync-linux-amd64` — Linux (x64)
- `arrsync-linux-arm64` — Linux (ARM)
- `arrsync-darwin-amd64` — macOS (Intel)
- `arrsync-darwin-arm64` — macOS (Apple Silicon)
- `checksums.txt` — SHA256 checksums for all assets

Example: download, verify and run (Linux)
```bash
curl -LO https://github.com/mayvqt/ArrSync/releases/download/v1.0.0/arrsync-linux-amd64
curl -LO https://github.com/mayvqt/ArrSync/releases/download/v1.0.0/checksums.txt
sha256sum -c checksums.txt
chmod +x arrsync-linux-amd64
./arrsync-linux-amd64
```

**Option B: Build from Source**
```bash
cd src
go build -o arrsync.exe .
```

### 2️⃣ Configure

Create a `.env` file (copy from `.env.example`):

```env
# Required
OVERSEER_URL=http://192.168.1.21:5055
OVERSEER_API_KEY=your-api-key-here

# Optional
PORT=8080
API_TIMEOUT=30
MAX_RETRIES=3
DRY_RUN=false
LOG_LEVEL=info
```

**Get your Overseer API Key:**  
Settings → General → API Key

### 3️⃣ Run

```bash
./arrsync
```

You should see:
```
INFO[0000] Validating Overseer connection...
INFO[0000] Overseer connection validated successfully
INFO[0000] ArrSync server started                        port=8080
```

### 4️⃣ Configure Webhooks

#### **Sonarr**
1. Settings → Connect → Add Webhook
2. **URL:** `http://your-server:8080/webhook/sonarr`
3. **Triggers:** ☑️ On Series Delete
4. **Method:** POST
5. Test & Save

#### **Radarr**  
1. Settings → Connect → Add Webhook
2. **URL:** `http://your-server:8080/webhook/radarr`
3. **Triggers:** ☑️ On Movie Delete
4. **Method:** POST
5. Test & Save

---

## ⚙️ Configuration

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `OVERSEER_URL` | Overseer base URL | - | ✅ |
| `OVERSEER_API_KEY` | Overseer API key | - | ✅ |
| `PORT` | Server port | `8080` | ❌ |
| `API_TIMEOUT` | API timeout (seconds) | `30` | ❌ |
| `MAX_RETRIES` | Max retry attempts | `3` | ❌ |
| `DRY_RUN` | Test mode (no deletions) | `false` | ❌ |
| `LOG_LEVEL` | Logging level | `info` | ❌ |

**Log Levels:** `debug`, `info`, `warn`, `error`

---

## 🔌 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/webhook/sonarr` | POST | Sonarr webhook receiver |
| `/webhook/radarr` | POST | Radarr webhook receiver |
| `/health` | GET | Health check + Overseer status |

**Health Check Response:**
```json
{
  "status": "healthy",
  "service": "arrsync",
  "healthy": true,
  "overseer": "available"
}
```

---

## 🧪 Testing

### Dry-Run Mode

Test without deleting anything:
```bash
DRY_RUN=true ./arrsync
```

Logs will show:
```
WARN[0000] DRY RUN MODE ENABLED - No deletions will be performed
WARN[0042] [DRY RUN] Would delete media from Overseer    mediaId=1636 tmdbId=249042
```

### Manual Testing

```bash
# Test Sonarr webhook
curl -X POST http://localhost:8080/webhook/sonarr \
  -H "Content-Type: application/json" \
  -d '{"eventType": "Test"}'

# Test health endpoint
curl http://localhost:8080/health
```

---

## 🏗️ Building

```bash
# Current platform
cd src
go build -o arrsync.exe .

# Linux
GOOS=linux GOARCH=amd64 go build -o arrsync .

# Windows
GOOS=windows GOARCH=amd64 go build -o arrsync.exe .

# macOS
GOOS=darwin GOARCH=amd64 go build -o arrsync .
```

---

## 🐳 Docker

**Dockerfile:**
```dockerfile
FROM golang:1.21-alpine AS builder
WORKDIR /app
COPY src/ .
RUN go mod download && go build -o arrsync .

FROM alpine:latest
RUN apk --no-cache add ca-certificates
WORKDIR /app
COPY --from=builder /app/arrsync .
EXPOSE 8080
CMD ["./arrsync"]
```

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  arrsync:
    build: .
    ports:
      - "8080:8080"
    environment:
      OVERSEER_URL: http://overseer:5055
      OVERSEER_API_KEY: ${OVERSEER_API_KEY}
      LOG_LEVEL: info
    restart: unless-stopped
```

---

## 🔧 Troubleshooting

### Overseer Connection Failed
- ✅ Verify `OVERSEER_URL` is correct
- ✅ Check Overseer is running: `curl http://your-overseer:5055/api/v1/status`
- ✅ Verify API key in Overseer Settings

### Webhooks Not Working
- ✅ Check ArrSync is reachable from Sonarr/Radarr
- ✅ Test webhook in Sonarr/Radarr settings
- ✅ Check ArrSync logs: `LOG_LEVEL=debug ./arrsync`

### Media Not Deleted
- ✅ Check if `DRY_RUN=true` is enabled
- ✅ Verify media exists in Overseer
- ✅ Check logs for TMDB ID matching

---

## 📂 Project Structure

```
ArrSync/
├── src/
│   ├── main.go                    # Entry point
│   ├── go.mod                     # Dependencies
│   ├── internal/
│   │   ├── config/
│   │   │   └── config.go          # Configuration loading
│   │   ├── handlers/
│   │   │   └── webhook.go         # HTTP handlers
│   │   ├── models/
│   │   │   ├── webhooks.go        # Webhook structures
│   │   │   └── overseer.go        # Overseer API models
│   │   └── services/
│   │       ├── overseer.go        # Overseer API client
│   │       └── cleanup.go         # Deletion logic
├── .env.example                   # Example config
├── .gitignore
└── README.md
```

---

## 🤝 Contributing

Contributions welcome! Please:

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push: `git push origin feature/amazing-feature`
5. Open a Pull Request

---

## 📜 License

MIT License - See [LICENSE](LICENSE) for details

---

## 💬 Support

- **Issues:** [GitHub Issues](https://github.com/mayvqt/ArrSync/issues)
- **Discussions:** [GitHub Discussions](https://github.com/mayvqt/ArrSync/discussions)

---

**Made with ❤️ for the *arr stack community**
