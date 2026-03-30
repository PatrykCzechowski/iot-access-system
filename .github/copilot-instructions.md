# Project Guidelines — AccessControl

IoT access control system: .NET 10 API + Blazor WASM UI + ESP32 firmware + MQTT.

## Architecture

**Backend** — Clean Architecture + CQRS + MediatR:

```
Domain (entities, value objects, exceptions)
  ↑
Application (commands, queries, DTOs, validators, interfaces)
  ↑
Infrastructure (EF Core, Identity, MQTT, device discovery)
  ↑
Api (minimal API endpoints, global exception handler)
```

- Every user action flows through MediatR: `Endpoint → IRequest → Handler`
- Commands mutate state, queries read — keep them separated in feature folders (`Auth/`, `Cards/`, `Devices/`)
- Validation via FluentValidation + `ValidationBehavior` pipeline — validators run before handlers
- `GlobalExceptionHandler` maps domain exceptions to RFC 7807 ProblemDetails

**UI** — Blazor WebAssembly (client-side) with MudBlazor v9 (Material Design 3):

- HTTP via Flurl.Http with auto-injected Bearer token
- Auth state from JWT parsed client-side (`CustomAuthStateProvider`)
- Token stored in browser LocalStorage (Blazored)
- UI labels in **English** ("Login", "Log in", "Devices", etc.)

**Firmware** — ESP32-S3 (Arduino Nano ESP32) via PlatformIO:

- Two device types: `card-reader` (RFID RC522) and `lock-controller` (relay)
- Shared libraries in `firmware/shared/` — WiFi, MQTT, mDNS, device config, device ID
- Communication: MQTT topics pattern `accesscontrol/{hwid}/{feature}/{action}`
- WiFi provisioning via captive portal (WiFiManager); mDNS discovery for broker

## Code Style

### C# (.NET)

- **Minimal APIs** — endpoints grouped by feature in `Endpoints/` using `MapGroup()`
- **CQRS naming**: `Create[Resource]Command`, `Get[Resource]Query`, `[Resource]Dto`
- Validators: `[Command]Validator` — one per command, co-located in same folder
- EF Core snake_case naming convention (`UseSnakeCaseNamingConvention()`)
- Entity creation via factory methods (e.g., `AccessCard.Create(...)`)
- MQTT handlers implement `IMqttMessageHandler` with `CanHandle(topic)` regex + `HandleAsync()`

### C++ (Firmware)

- PlatformIO build includes `../shared/*.cpp` via `build_src_filter`
- Device config persisted in ESP32 NVS (Preferences API)
- Hardware IDs: deterministic UUID v5-like from MAC address
- Keep pin definitions in `config.h`, shared constants in `config_common.h`

## Build and Test

```bash
# Backend — run locally
cd src/AccessControl.Api
dotnet run

# Backend — Docker (API + PostgreSQL + Mosquitto)
docker compose up --build

# Firmware — build & upload (PlatformIO CLI)
cd firmware/card-reader   # or lock-controller
pio run                   # build
pio run -t upload         # flash

# EF Core migrations
cd src/AccessControl.Api
dotnet ef migrations add <Name> -p ../AccessControl.Infrastructure -s .
```

Auto-migration runs on startup in Development environment. Admin account seeded automatically.

No test projects exist yet — when adding tests, follow xUnit + FluentAssertions conventions.

## Conventions

- **Endpoint authorization**: All card/device endpoints require `Admin` role via `.RequireAuthorization(p => p.RequireRole("Admin"))`
- **Rate limiting**: Auth endpoints use `"auth"` rate limiter (10 req/min)
- **MQTT topic namespace**: Always prefix with `accesscontrol/`; handler routing uses regex on topic string
- **Device discovery**: mDNS service type `_accesscontrol._tcp` with TXT records (`hwid`, `model`, `mac`, `features`, `fw`)
- **Config secrets**: Use `dotnet user-secrets` locally; environment variables in Docker (see `docker-compose.yml`)
- **Health endpoint**: `/health` — includes DB context check
- **API docs**: Scalar UI at `/scalar/v1` (development)

## Key Files

| Purpose | Path |
|---------|------|
| API composition root | `src/AccessControl.Api/Program.cs` |
| Application DI | `src/AccessControl.Application/DependencyInjection.cs` |
| Infrastructure DI | `src/AccessControl.Infrastructure/DependencyInjection.cs` |
| MQTT background service | `src/AccessControl.Infrastructure/Mqtt/MqttClientService.cs` |
| MQTT topic constants | `src/AccessControl.Application/Common/MqttTopics.cs` |
| MQTT message handlers | `src/AccessControl.Infrastructure/Mqtt/Handlers/` |
| EF DbContext | `src/AccessControl.Infrastructure/Persistence/AccessControlDbContext.cs` |
| Domain entities | `src/AccessControl.Domain/Entities/` |
| Firmware shared libs | `firmware/shared/` |
| Docker infrastructure | `docker-compose.yml`, `mosquitto/` |
