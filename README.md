# Contoso.NotificationRelay.Legacy

A .NET Framework 4.7.2 notification relay service that consumes messages from a queue and dispatches notifications via pluggable provider interfaces (Email, SMS, Teams).

> **Migration context:** This repo represents the legacy pre-migration state. See [`docs/migration-plan.md`](docs/migration-plan.md) for the planned migration to .NET 8 Worker Service.

## Solution Structure

```
├── src/
│   ├── NotificationRelay.Service          # Console / Windows Service entry point
│   ├── NotificationRelay.Application      # Retry policy, deduplication, dispatch orchestration
│   ├── NotificationRelay.Domain           # Domain model & interfaces
│   └── NotificationRelay.Infrastructure   # In-memory adapters (queue, providers, dedupe, logging)
├── tests/
│   └── NotificationRelay.UnitTests        # MSTest unit tests
├── docs/                                  # Migration plan & architecture docs
└── build/                                 # CI placeholder
```

## Quick Start

```bash
# Build
dotnet build Contoso.NotificationRelay.Legacy.sln

# Run (console mode — processes seeded in-memory messages then idles)
dotnet run --project src/NotificationRelay.Service

# Run tests
dotnet test Contoso.NotificationRelay.Legacy.sln
```

### Windows Service Mode

Pass `--service` to run as a Windows Service (requires `sc.exe` registration on Windows):

```
NotificationRelay.Service.exe --service
```

## Configuration

Settings are in `src/NotificationRelay.Service/App.config`:

| Key | Default | Description |
|-----|---------|-------------|
| `PollIntervalMs` | `1000` | Queue polling interval in milliseconds |
| `RetryCount` | `3` | Max retry attempts per message |
| `RetryBackoffMs` | `200` | Initial backoff delay in milliseconds (doubles each retry) |
| `LogDirectory` | `logs` | Directory for rolling log files |

## Architecture

The service uses a layered architecture with clear boundaries:

- **Domain** — `NotificationMessage` model, `IQueueConsumer`, `IEmailSender`, `ISmsSender`, `ITeamsSender`, `IDeduplicationStore`
- **Application** — `NotificationDispatcher` orchestrates retry + dedupe + routing; `RetryPolicy` handles exponential backoff
- **Infrastructure** — In-memory implementations of all interfaces; `FileAndConsoleLogger` for dual-output logging
- **Service** — Entry point with console/Windows Service dual-mode support
