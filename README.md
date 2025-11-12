ArrSync listens for deletion webhooks from Sonarr and Radarr and forwards the relevant information to an Overseer instance.

- Supported events: Sonarr/Radarr delete/remove events (native webhook payloads).
- Validation: optional secret/HMAC signature verification for incoming webhooks to ensure authenticity.
- Transformation: extracts the fields Overseer expects (title, release date, IDs, path, and deletion reason) and converts payloads when necessary.
- Delivery: posts a compact deletion record to the configured Overseer API endpoint; supports retry/backoff on transient failures.
- Observability and health: exposes a health endpoint and Prometheus metrics for monitoring delivery success and failure counts.
- Configuration: all behavior is configured via `appsettings.{Environment}.json` in `src/ArrSync.App` (Overseer endpoint, retry policy, secrets, logging, and metrics).

Use case: keep Overseer's database in sync with Sonarr/Radarr by ensuring removals performed in those apps are reflected in Overseer without duplicating other event types.

## Requirements

- .NET 8 SDK
- A Unix-like shell (bash) or Windows PowerShell for the commands below

## Quick start

Build the solution from the repository root:

```bash
dotnet build ArrSync.sln -c Release
```

Run the tests:

```bash
dotnet test ArrSync.sln -c Release
```

Run the app (from the `src/ArrSync.App` folder):

```bash
cd src/ArrSync.App
dotnet run --configuration Release
```

Configuration files (`appsettings.Development.json`, `appsettings.Production.json`) live in `src/ArrSync.App` and can be used to set endpoints, secrets, and monitoring options.

## Contributing

Small, focused pull requests are welcome. Follow the project's coding conventions and include tests for new features or bug fixes.

## License

This project is provided under the terms of its existing license. See the repository for details.
