# Contoso.NotificationRelay

A .NET 8 notification relay Worker Service that consumes messages from a queue and dispatches notifications via pluggable provider interfaces (Email, SMS, Teams).

> **Migration context:** This service was migrated from .NET Framework 4.7.2 to a .NET 8 Generic Host Worker Service. See [`docs/migration-plan.md`](docs/migration-plan.md) for the migration details.

## Solution Structure

```
├── src/
│   ├── NotificationRelay.Service          # Generic Host entry point (BackgroundService + /healthz)
│   ├── NotificationRelay.Application      # Retry policy, deduplication, dispatch orchestration
│   ├── NotificationRelay.Domain           # Domain model & interfaces
│   └── NotificationRelay.Infrastructure   # In-memory adapters (queue, providers, dedupe) + DI
├── tests/
│   ├── NotificationRelay.UnitTests        # xUnit unit tests
│   └── NotificationRelay.IntegrationTests # WebApplicationFactory end-to-end tests
├── docs/                                  # Migration plan & architecture docs
└── build/                                 # CI pipeline (dotnet CLI on Linux)
```

## Quick Start

```bash
# Build
dotnet build Contoso.NotificationRelay.Legacy.sln

# Run (processes seeded in-memory messages then idles; exposes /healthz)
dotnet run --project src/NotificationRelay.Service

# Run tests
dotnet test Contoso.NotificationRelay.Legacy.sln
```

### Windows Service Mode

The host calls `UseWindowsService()`, so the same executable runs under the Windows
Service Control Manager when installed as a service (no-op on other platforms):

```
sc create ContosoNotificationRelay binPath= "C:\path\NotificationRelay.Service.exe"
```

### Health Check

A health endpoint is exposed at `GET /healthz` (returns `Healthy`).

## Configuration

Settings live in `src/NotificationRelay.Service/appsettings.json` under the `Relay`
section, bound to `RelaySettings` via `IOptions<T>` (environment overrides via
`appsettings.{Environment}.json` and environment variables):

| Key | Default | Description |
|-----|---------|-------------|
| `Relay:PollIntervalMs` | `1000` | Queue polling interval in milliseconds |
| `Relay:RetryCount` | `3` | Max retry attempts per message |
| `Relay:RetryBackoffMs` | `200` | Initial backoff delay in milliseconds (doubles each retry) |

Logging is handled by `Microsoft.Extensions.Logging` (console provider); the legacy
`LogDirectory` file-logging setting no longer applies.

## Architecture

The service uses a layered architecture with clear boundaries:

- **Domain** — `NotificationMessage` model, `IQueueConsumer`, `IEmailSender`, `ISmsSender`, `ITeamsSender`, `IDeduplicationStore`
- **Application** — `NotificationDispatcher` orchestrates retry + dedupe + routing; `RetryPolicy` handles exponential backoff
- **Infrastructure** — In-memory implementations of all interfaces; DI registration extension (`AddNotificationRelayServices`)
- **Service** — Generic Host entry point (`Program.cs` + `RelayWorker : BackgroundService`) with health checks
