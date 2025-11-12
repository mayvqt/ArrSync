# csharparrsync

This is a .NET 8 scaffold of ArrSync (console/hosted app) intended as a starting point for porting the original Go project.

Build

```bash
cd csharparrsync/src/ArrSync.App
dotnet build
```

Run

```bash
# set required env vars before running
export OVERSEER_URL="http://localhost:5055"
export OVERSEER_API_KEY="your_api_key"
dotnet run --project src/ArrSync.App
```

Endpoints

- POST /webhook/sonarr — Sonarr deletion webhook
- POST /webhook/radarr — Radarr deletion webhook
- GET /health — Aggregated health including Overseerr

Notes

- The app is implemented as a hostable console application using the ASP.NET Core minimal APIs so it can accept HTTP webhook calls while remaining a general-purpose executable for future features.
- Configuration is driven by environment variables. See `Program.cs` for keys.

Configuration
-------------
You can configure the app using environment variables or the `ArrSync:Config` section in `appsettings.json`.

Important environment variables / config keys:
- OVERSEER_URL or ArrSync:Config:Url — Overseerr base URL (default: http://localhost:5055)
 - OVERSEER_URL or ArrSync:Config:OverseerUrl — Overseerr base URL (default: http://localhost:5055)
- OVERSEER_API_KEY or ArrSync:Config:ApiKey — Overseerr API key (optional for dev)
- TIMEOUT_SECONDS or ArrSync:Config:TimeoutSeconds — HTTP client timeout in seconds (default: 10)
- MAX_RETRIES or ArrSync:Config:MaxRetries — Max retry attempts for Overseerr calls (default: 3)
- INITIAL_BACKOFF or ArrSync:Config:InitialBackoffSeconds — Initial backoff in seconds (default: 1)
- DRY_RUN or ArrSync:Config:DryRun — When true, actions will be logged but not performed (default: false)
- MONITOR_INTERVAL_SECONDS or ArrSync:Config:MonitorIntervalSeconds — Health poll interval for Overseerr monitor (default: 60)
- WEBHOOK_PORT or ArrSync:Config:Port — Port the app listens on for incoming webhooks (default: 5011 if set in appsettings)
- WEBHOOK_SECRET or ArrSync:Config:WebhookSecret — Optional secret that webhook providers must send in the `X-Webhook-Secret` header. When set, incoming webhooks without the header or with the wrong value will be rejected (recommended in production).

Example (bash):

```bash
export OVERSEER_URL="http://localhost:5055"
export OVERSEER_API_KEY="your_api_key"
export DRY_RUN=true
export WEBHOOK_PORT=5011
export WEBHOOK_SECRET="my-super-secret"
dotnet run --project src/ArrSync.App
```

For development you can edit `src/ArrSync.App/appsettings.Development.json` which is included in the scaffold.
