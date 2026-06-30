# syntax=docker/dockerfile:1.7

## ---------------------------------------------------------------------------
## Build stage: restore + publish the Api project (and everything it depends
## on) using the pinned .NET 10 SDK.
## ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution-level files first so Docker can cache the restore layer
# whenever only application code (not dependencies) changes.
COPY Directory.Build.props Directory.Packages.props AI.DocumentIntelligence.sln ./
COPY src/AI.DocumentIntelligence.Api/AI.DocumentIntelligence.Api.csproj src/AI.DocumentIntelligence.Api/
COPY src/AI.DocumentIntelligence.Application/AI.DocumentIntelligence.Application.csproj src/AI.DocumentIntelligence.Application/
COPY src/AI.DocumentIntelligence.Domain/AI.DocumentIntelligence.Domain.csproj src/AI.DocumentIntelligence.Domain/
COPY src/AI.DocumentIntelligence.Infrastructure/AI.DocumentIntelligence.Infrastructure.csproj src/AI.DocumentIntelligence.Infrastructure/
COPY src/AI.DocumentIntelligence.Persistence/AI.DocumentIntelligence.Persistence.csproj src/AI.DocumentIntelligence.Persistence/
COPY src/AI.DocumentIntelligence.Tests/AI.DocumentIntelligence.Tests.csproj src/AI.DocumentIntelligence.Tests/

RUN dotnet restore src/AI.DocumentIntelligence.Api/AI.DocumentIntelligence.Api.csproj

# Now copy the rest of the source and publish.
COPY src/ src/
RUN dotnet publish src/AI.DocumentIntelligence.Api/AI.DocumentIntelligence.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

## ---------------------------------------------------------------------------
## Runtime stage: minimal ASP.NET runtime image only.
## ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is used solely by HEALTHCHECK below to probe the /health endpoint.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup --shell /bin/false appuser

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish ./

USER appuser
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "AI.DocumentIntelligence.Api.dll"]
