# syntax=docker/dockerfile:1

# ─── Build Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/AccessControl.Api/AccessControl.Api.csproj",             "src/AccessControl.Api/"]
COPY ["src/AccessControl.Application/AccessControl.Application.csproj", "src/AccessControl.Application/"]
COPY ["src/AccessControl.Infrastructure/AccessControl.Infrastructure.csproj", "src/AccessControl.Infrastructure/"]
COPY ["src/AccessControl.Domain/AccessControl.Domain.csproj",       "src/AccessControl.Domain/"]

RUN dotnet restore "src/AccessControl.Api/AccessControl.Api.csproj"

COPY . .
RUN dotnet publish "src/AccessControl.Api/AccessControl.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Runtime Stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "AccessControl.Api.dll"]
