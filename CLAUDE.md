# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build
dotnet run --project GrabAndGo/GrabAndGo.Api.csproj
```

Before running, set the SQL Server connection string in `GrabAndGo/appsettings.json` (`ConnectionStrings:DefaultConnection`) and optionally override the JWT key via the `GRABANDGO_JWT_KEY` environment variable.

There are no automated tests. Use `GrabAndGo/GrabAndGo.http` for manual endpoint testing.

## Architecture

4-project .NET 8 solution with strict separation of concerns:

| Project | Role |
|---|---|
| `GrabAndGo.Api` | Controllers, SignalR hubs, background services, DI wiring (`Program.cs`) |
| `GrabAndGo.Services` | Business logic — injects only Repository interfaces |
| `GrabAndGo.DataAccess` | Raw ADO.NET repositories + `SqlExecutor` core — no EF Core |
| `GrabAndGo.Models` | Request/Response DTOs only — no domain entities |

Dependency direction: `Api → Services → DataAccess → Models` (all layers reference `Models`).

## Critical Rules

### No Entity Framework — Ever
All DB access goes through `GrabAndGo.DataAccess/Core/SqlExecutor.cs` via three methods:
- `ExecuteReaderAsync<T>()` — SELECT returning a `List<T>` via `FOR JSON PATH`
- `ExecuteNonQueryAsync<T>()` — INSERT/UPDATE; serializes the DTO to `@P_JSON_REQUEST`
- `ExecuteScalarAsync<T>()` — scalar values (counts, IDs)

### Stored Procedure JSON Contract
Every SP must follow this pattern exactly:
- **List returns**: `FOR JSON PATH, INCLUDE_NULL_VALUES`
- **Single object returns**: `FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES`
- `INCLUDE_NULL_VALUES` is mandatory — the Flutter client requires all keys present even when null
- Ingestion uses `OPENJSON` on a single `@P_JSON_REQUEST` parameter
- `@Parameter` names must exactly match C# property names (case-sensitive, reflection-based)
- All PKs/FKs are `INT IDENTITY(1,1)` — no GUIDs

### DI & Interface Rules
- `SqlExecutor` → `Singleton`; all Repositories and Services → `Scoped`
- Controllers inject only Service interfaces; Services inject only Repository interfaces
- Every Service and Repository must have a matching interface — no direct implementation injection
- No `static` methods for DB access or business logic

### DTOs
- Every request: `[Feature]RequestDto` with Data Annotations (`[Required]`, `[EmailAddress]`, etc.)
- Every response: `[Feature]ResponseDto` — never expose internal/sensitive fields (e.g., password hashes)
- DTO property names must exactly match DB column names (used in `OPENJSON` and JSON serialization)

## Feature Development Workflow

Always build features in this order (inside-out):
1. Define Request/Response DTOs in `GrabAndGo.Models`
2. Write the T-SQL Stored Procedure
3. Write the Repository interface + implementation
4. Write the Service interface + implementation
5. Write the Controller endpoint

## Communication

Always explain the underlying logic step-by-step before modifying any files. Treat this as a learning environment and never provide quick fixes or skip steps to save time.

## Key Components

**Authentication**: JWT Bearer (7-day expiry), BCrypt password hashing. Key loaded from `GRABANDGO_JWT_KEY` env var, falling back to `appsettings.json`.

**Real-time**: SignalR hub at `/hubs/cart` sends cart updates grouped by `SessionId`. Two notification services: `SignalRCartNotificationService` and `GateNotificationService`.

**MQTT**: `MqttVisionWorker` background service connects to HiveMQ (`broker.hivemq.com:1883`), subscribes to `grabandgo/+/store/+/vision/events/#`, and processes vision events (item pick/return) then broadcasts via SignalR.

**Gate safety**: `POST /api/gate/checkout` always returns HTTP 200 regardless of business outcome — hardware must never be left in an error state.
