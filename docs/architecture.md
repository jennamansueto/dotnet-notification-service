# Architecture — Contoso Notification Relay Service

## Overview

The Notification Relay Service is a background processing service that:

1. Polls a message queue for `NotificationMessage` items.
2. Deduplicates messages (by `MessageId`) to ensure at-most-once processing.
3. Dispatches each message to the appropriate provider (Email, SMS, or Teams).
4. Retries transient failures with exponential backoff.

## Layer Diagram

```
┌─────────────────────────────────┐
│   NotificationRelay.Service     │  ← Entry point (Console / Windows Service)
│   - Program.cs (Main)           │
│   - NotificationRelayEngine     │  ← Wires up dependencies, runs poll loop
│   - NotificationRelayWinService │  ← ServiceBase wrapper
│   - AppSettings                 │  ← Reads App.config
└──────────────┬──────────────────┘
               │
┌──────────────▼──────────────────┐
│   NotificationRelay.Application │  ← Business logic orchestration
│   - NotificationDispatcher      │  ← Dedupe check → retry → route by type
│   - RetryPolicy                 │  ← Exponential backoff retry
└──────────────┬──────────────────┘
               │
┌──────────────▼──────────────────┐
│   NotificationRelay.Domain      │  ← Pure domain model + interfaces (ports)
│   - NotificationMessage         │
│   - NotificationType            │
│   - IQueueConsumer              │
│   - IEmailSender / ISmsSender   │
│   - ITeamsSender                │
│   - IDeduplicationStore         │
│   - ILogger                     │
└──────────────┬──────────────────┘
               │ (implemented by)
┌──────────────▼──────────────────┐
│   NotificationRelay.Infra       │  ← Adapters (implementations)
│   - InMemoryQueueConsumer       │  ← ConcurrentQueue-based
│   - InMemoryEmailSender         │
│   - InMemorySmsSender           │
│   - InMemoryTeamsSender         │
│   - InMemoryDeduplicationStore  │  ← ConcurrentDictionary-based
│   - FileAndConsoleLogger        │  ← Dual-output logging
│   - ConsoleOnlyLogger           │  ← Lightweight test logger
└─────────────────────────────────┘
```

## Dependency Rules

- **Domain** has zero project dependencies — only `System.*` types.
- **Application** depends only on **Domain**.
- **Infrastructure** depends only on **Domain** (implements its interfaces).
- **Service** depends on **Application** and **Infrastructure** (composes the system).
- **Tests** depend on **Application** and **Infrastructure**.

## Configuration

All settings live in `App.config` under `<appSettings>`:

- `PollIntervalMs` — how often the worker checks the queue (default: 1000ms).
- `RetryCount` — max retry attempts per message (default: 3).
- `RetryBackoffMs` — initial backoff delay, doubled each retry (default: 200ms).
- `LogDirectory` — relative or absolute path for log files (default: `logs`).

## Threading Model

The service runs a single background thread (`NotificationRelayEngine.PollLoop`) that:

1. Calls `IQueueConsumer.Dequeue()` — returns `null` if empty.
2. If null, sleeps for `PollIntervalMs` and loops.
3. If a message is found, passes it to `NotificationDispatcher.Dispatch()`.
4. Dispatch is synchronous and blocking — retries use `Thread.Sleep`.

**Known limitation:** The single-threaded model limits throughput. A future improvement would be to use a thread pool or async processing (addressed in .NET 8 migration).

## Logging

Custom `ILogger` interface with four levels: `Debug`, `Info`, `Warn`, `Error`.

- **FileAndConsoleLogger** — writes timestamped lines to both `Console.WriteLine` and a rolling log file in the configured `LogDirectory`.
- **ConsoleOnlyLogger** — lightweight implementation for tests (no file I/O).

Log format: `yyyy-MM-dd HH:mm:ss.fff [LEVEL] message`

**Known limitation:** No structured logging. CorrelationId is embedded in message strings, not in a log scope. This is addressed in the .NET 8 migration.
