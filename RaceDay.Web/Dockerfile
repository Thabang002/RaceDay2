# syntax=docker/dockerfile:1
# =====================================================================
# RaceDay.Web - ASP.NET Core MVC front end
# Multi-stage build: the SDK image compiles, the small ASP.NET runtime image runs.
# Build context = this folder (RaceDay.Web).
# =====================================================================

# ---------- 1. Build & publish ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, using only the project file, so this layer is cached
# until package references change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

# Then copy the source and publish a Release build.
COPY . .
RUN dotnet publish RaceDay.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------- 2. Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Listen on 8080 inside the container (default non-root port for .NET 8+ images).
# Api__BaseUrl points at the RaceDay API running on the host machine; override with -e if needed.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Api__BaseUrl=http://host.docker.internal:5163/ \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the built-in non-root "app" user.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
