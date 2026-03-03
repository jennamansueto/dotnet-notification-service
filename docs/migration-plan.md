# Migration Plan: .NET Framework 4.7.2 → .NET 8 Worker Service

## Current State (This Repo)

The Notification Relay Service runs on .NET Framework 4.7.2 as a console application with optional Windows Service mode (`ServiceBase`). Key characteristics:

- **Hosting:** Console app with `--service` flag for Windows Service mode via `ServiceBase`.
- **Threading:** Manual `Thread` with a polling loop and `Thread.Sleep` for backoff.
- **Configuration:** `App.config` with `<appSettings>` section, read via `ConfigurationManager`.
- **Dependency Injection:** None — "poor man's DI" with manual `new` in the engine constructor.
- **Logging:** Custom `ILogger` abstraction writing to console + rolling text file.
- **Queue:** Abstracted behind `IQueueConsumer`; current implementation is in-memory.
- **Tests:** MSTest with synchronous test methods.

## Target State (.NET 8 Worker Service)

### 1. Hosting — Generic Host + BackgroundService

| Before | After |
|--------|-------|
| `static void Main` + `Thread` loop | `Host.CreateDefaultBuilder` + `BackgroundService` |
| `ServiceBase` for Windows Service | `UseWindowsService()` / `UseSystemd()` extensions |
| Manual thread lifecycle | `CancellationToken`-based graceful shutdown |

**Key change:** Replace `NotificationRelayEngine` with a class inheriting `BackgroundService`. The `ExecuteAsync` method replaces the manual `PollLoop`. Remove `ServiceBase` subclass entirely.

### 2. Configuration — appsettings.json + IOptions

| Before | After |
|--------|-------|
| `App.config` XML | `appsettings.json` + environment overrides |
| `ConfigurationManager.AppSettings[key]` | `IOptions<T>` / `IConfiguration` |
| Config transforms (App.Release.config) | `appsettings.Production.json` + env vars |

**Key change:** Delete `App.config` and `AppSettings.cs`. Create a POCO settings class bound via `services.Configure<RelaySettings>(config.GetSection("Relay"))`.

### 3. Dependency Injection — Built-in DI

| Before | After |
|--------|-------|
| `new InMemoryQueueConsumer(logger)` in engine | `services.AddSingleton<IQueueConsumer, InMemoryQueueConsumer>()` |
| Manual wiring in `NotificationRelayEngine` | `Program.cs` service registration |

**Key change:** Register all interfaces and implementations in `Program.cs`. Constructor injection everywhere. Remove `NotificationRelayEngine` — the `BackgroundService` takes `IQueueConsumer` and `NotificationDispatcher` via DI.

### 4. Logging — Microsoft.Extensions.Logging

| Before | After |
|--------|-------|
| Custom `ILogger` interface | `Microsoft.Extensions.Logging.ILogger<T>` |
| `string.Format(...)` messages | Structured logging with message templates |
| `FileAndConsoleLogger` class | Built-in console + file providers (or Serilog) |
| No correlation ID in log scope | `ILogger.BeginScope` for CorrelationId propagation |

**Key change:** Delete `Domain.Interfaces.ILogger`, `FileAndConsoleLogger`, and `ConsoleOnlyLogger`. Use `ILogger<T>` from `Microsoft.Extensions.Logging` everywhere. Add JSON console formatter for container environments.

### 5. Async/Await

| Before | After |
|--------|-------|
| Synchronous `void Send(...)` | `Task SendAsync(...)` |
| `Thread.Sleep` for backoff | `Task.Delay` with `CancellationToken` |
| Blocking `Dequeue()` | `async Task<NotificationMessage?> DequeueAsync()` |

**Key change:** Make all interfaces async. Replace `Thread.Sleep` with `Task.Delay`. Use `Channel<T>` instead of `ConcurrentQueue<T>` for the in-memory queue.

### 6. Health Checks

| Before | After |
|--------|-------|
| None | ASP.NET Core health endpoint at `/healthz` |

**Key change:** Use `WebApplication.CreateBuilder` to host both the worker and a minimal health endpoint. Add `builder.Services.AddHealthChecks()` and `app.MapHealthChecks("/healthz")`.

### 7. Testing

| Before | After |
|--------|-------|
| MSTest, synchronous | xUnit (or MSTest), async tests |
| `ConsoleOnlyLogger` test helper | `NullLogger<T>` from Microsoft.Extensions.Logging |

### 8. Project Structure Changes

- `.csproj` files: Already SDK-style, change `<TargetFramework>` from `net472` to `net8.0`.
- Remove `Microsoft.NETFramework.ReferenceAssemblies` NuGet package.
- Remove `System.Configuration.ConfigurationManager` package.
- Add `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Logging` packages.
- Remove `App.config`, add `appsettings.json`.
- Change `LangVersion` from `7.3` to `12`.

## Migration Steps (Ordered)

1. **Update target framework** in all `.csproj` files: `net472` → `net8.0`.
2. **Remove** `Microsoft.NETFramework.ReferenceAssemblies` package references.
3. **Replace** custom `ILogger` with `Microsoft.Extensions.Logging.ILogger<T>`.
4. **Make interfaces async** (`Send` → `SendAsync`, `Dequeue` → `DequeueAsync`).
5. **Replace** `App.config` + `AppSettings.cs` with `appsettings.json` + `IOptions<T>`.
6. **Replace** `NotificationRelayEngine` + `ServiceBase` with `BackgroundService`.
7. **Wire up DI** in `Program.cs` using `WebApplication.CreateBuilder`.
8. **Add health checks** endpoint.
9. **Modernize C#** — file-scoped namespaces, records, nullable reference types.
10. **Update tests** — async, replace `ConsoleOnlyLogger` with `NullLogger<T>`.
11. **Verify** — build, run, test on .NET 8.

## Risks & Considerations

- **ServiceBase removal** — any deployment scripts using `sc.exe` need updating.
- **App.config consumers** — check if any ops tooling reads App.config directly.
- **Thread.Sleep in retry** — must be replaced with async `Task.Delay` to avoid blocking thread pool threads.
- **Custom ILogger** — any external code depending on `Domain.Interfaces.ILogger` must be updated.
