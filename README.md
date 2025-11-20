[![CI](https://github.com/mayvqt/ArrSync/actions/workflows/ci.yml/badge.svg)](https://github.com/mayvqt/ArrSync/actions)
[![CD](https://github.com/mayvqt/ArrSync/actions/workflows/release.yml/badge.svg)](https://github.com/mayvqt/ArrSync/actions)
[![Release](https://img.shields.io/github/v/release/mayvqt/ArrSync)](https://github.com/mayvqt/ArrSync/releases)
[![License](https://img.shields.io/github/license/mayvqt/ArrSync)](https://github.com/mayvqt/ArrSync/blob/main/LICENSE)

# ArrSync

ArrSync forwards Sonarr and Radarr deletion webhooks to an Overseer instance. It validates incoming webhooks (optional HMAC), extracts the relevant fields, and reliably posts compact deletion records to a configured Overseer API endpoint.

Features:
- Forwards Sonarr/Radarr delete/remove events
- Optional HMAC signature validation for authenticity
- Retry and backoff on transient delivery failures
- Health endpoint and Prometheus metrics for monitoring

Requirements:
- .NET 10
- PowerShell (Windows) or a POSIX shell for running commands

Quick start (from repository root):

```powershell
dotnet build ArrSync.sln -c Release
dotnet test ArrSync.sln -c Release
cd src/ArrSync.App; dotnet run --configuration Release
```

Configuration:
Edit `src/ArrSync.App/appsettings.Development.json` or `appsettings.Production.json` to set the Overseer endpoint, retry policy, secrets, and metrics settings.

Contributing:
Small, focused pull requests are welcome. Please include tests for new behavior and follow existing coding conventions.

License:
See the repository for license details.
