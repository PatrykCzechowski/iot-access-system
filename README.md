# AccessControl

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/get-docker/)

---

## Run with Docker Compose

```bash
cp .env.example .env
```

Edit `.env` and set your own values, then:

```bash
docker compose up --build
```

- **API:** http://localhost:8080
- **Health:** http://localhost:8080/health

---

## Run locally

### 1. Configure secrets

```bash
cd src/AccessControl.Api

dotnet user-secrets set "Jwt:Key" "<random-string-min-32-chars>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=access_db;Username=db_user;Password=<password>"
dotnet user-secrets set "Admin:Email" "admin@example.com"
dotnet user-secrets set "Admin:Password" "<password>"
```

### 2. Start

```bash
dotnet run --project src/AccessControl.Api
```

- **API:** https://localhost:7157
- **Scalar UI:** https://localhost:7157/scalar/v1
- **Health:** https://localhost:7157/health

Migrations and admin account seeding are applied automatically on startup.

---

