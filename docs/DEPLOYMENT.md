# Deployment Guide — AI Document Intelligence Platform

This guide covers everything a new engineer needs to deploy and operate the platform,
from local development through production.

---

## Table of contents

1. [Architecture overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Environment variables and secrets strategy](#3-environment-variables-and-secrets-strategy)
4. [Local development (Docker Compose)](#4-local-development-docker-compose)
5. [EF Core database migrations](#5-ef-core-database-migrations)
6. [Azure service setup](#6-azure-service-setup)
7. [CI/CD pipeline reference](#7-cicd-pipeline-reference)
8. [Production deployment](#8-production-deployment)
9. [Health checks and observability](#9-health-checks-and-observability)
10. [Scaling and performance](#10-scaling-and-performance)
11. [Backup and disaster recovery](#11-backup-and-disaster-recovery)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Architecture overview

```
Internet
    │
    ├── :80/:443 → frontend (nginx, Angular 20 SPA)
    │                  └── proxies /api/** → api service
    │
    └── :8080    → api (.NET 10, Clean Architecture)
                       ├── PostgreSQL + pgvector  (data + vector search)
                       ├── Azure Blob Storage     (document files)
                       ├── Azure OpenAI           (embeddings + chat)
                       └── Azure AI Search        (hybrid keyword + vector search)
```

In production, PostgreSQL and Azure Blob Storage are managed Azure services.
Azure AI Search has no local emulator; the API falls back to pgvector-only search
when no Azure Search endpoint is configured.

---

## 2. Prerequisites

### Local development

| Tool | Minimum version | Install |
|------|----------------|---------|
| Docker Desktop (or Engine + Compose plugin) | 26.x / Compose 2.27 | https://docs.docker.com/get-docker/ |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Node.js | 22 LTS | https://nodejs.org |
| Git | 2.x | https://git-scm.com |

### CI/CD

GitHub Actions runners (`ubuntu-latest`) provide all required tooling.
No self-hosted runner is necessary for the default configuration.

### Production deployment targets (choose one)

- **Azure Container Apps** (recommended) — fully managed, auto-scaling.
- **Azure Kubernetes Service (AKS)** — full control, bring your own cluster.
- **Docker Compose on a VM** — simplest option for small deployments.

---

## 3. Environment variables and secrets strategy

### Principle: no secrets in the repository

All runtime secrets are injected via environment variables.
The `.env.example` file documents every variable with safe placeholder values.
The real `.env` file is git-ignored and must never be committed.

### Variable hierarchy (highest wins)

```
1. Environment variables (set on host / container / CI secret)
2. appsettings.<Environment>.json
3. appsettings.json (baseline / defaults)
```

### Local development

```bash
cp .env.example .env
# Edit .env: set Jwt__SecretKey and any Azure credentials you need
```

### Development on a workstation (without Docker)

Use .NET user-secrets so secrets stay out of `appsettings.json`:

```bash
cd src/AI.DocumentIntelligence.Api
dotnet user-secrets set "Jwt:SecretKey"           "your-32-char-minimum-key"
dotnet user-secrets set "AzureOpenAI:ApiKey"      "your-azure-openai-key"
dotnet user-secrets set "AzureSearch:ApiKey"      "your-search-admin-key"
```

User-secrets are stored in `~/.microsoft/usersecrets/<project-guid>/secrets.json`
and are never included in builds or Docker images.

### CI/CD — GitHub repository secrets

Go to **Settings → Secrets and variables → Actions** and add:

| Secret name | Description |
|-------------|-------------|
| `DB_CONNECTION_STRING` | Full Npgsql connection string for the target database |
| `JWT_SECRET_KEY` | HS256 signing key, at least 32 characters (256 bits) |
| `AZURE_OPENAI_ENDPOINT` | `https://<resource>.openai.azure.com/` |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key |
| `AZURE_SEARCH_ENDPOINT` | `https://<resource>.search.windows.net` |
| `AZURE_SEARCH_API_KEY` | Azure AI Search admin key |
| `AZURE_STORAGE_CONNECTION` | Production blob storage connection string |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | (optional) Application Insights |

The `GITHUB_TOKEN` secret is automatically available and is used for
pushing images to GitHub Container Registry.

### Production — Azure Key Vault (recommended)

For production workloads, store secrets in Azure Key Vault and reference them
via the Key Vault references feature of your deployment target:

- **Azure Container Apps**: use managed identity + Key Vault secret references.
- **AKS**: use the Azure Key Vault Provider for Secrets Store CSI Driver.
- **App Service**: use Key Vault references in application settings.

Never expose raw secret values in compose files, Kubernetes manifests, or CI logs.

---

## 4. Local development (Docker Compose)

### First-time setup

```bash
# 1. Clone the repository
git clone https://github.com/<org>/ai-document-intelligence.git
cd ai-document-intelligence

# 2. Create your local env file
cp .env.example .env

# 3. Edit .env — at minimum, set a strong Jwt__SecretKey:
#    openssl rand -base64 48   (copy the output into .env)

# 4. Start the full stack
docker compose up -d

# 5. Verify all services are healthy
docker compose ps
```

### Service endpoints (once healthy)

| Service | URL |
|---------|-----|
| API (health) | http://localhost:8080/health |
| API (Swagger) | http://localhost:8080/swagger |
| Frontend | http://localhost:4200 |
| PostgreSQL | localhost:5432 |
| Azurite Blob | http://localhost:10000 |

### Useful compose commands

```bash
# Tail logs from all services
docker compose logs -f

# Tail logs from a specific service
docker compose logs -f api

# Restart a single service after code changes
docker compose up -d --build api

# Stop and remove containers (keep volumes)
docker compose down

# Stop and remove containers + wipe all volumes (destructive)
docker compose down -v
```

### Production-shaped local run (overlay)

```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

This applies production resource limits and restart policies while still using
the local Postgres and Azurite services.

---

## 5. EF Core database migrations

> **Status as of T17:** The `src/AI.DocumentIntelligence.Persistence` project uses
> stub repositories pending T02 (EF Core DbContext). Migration infrastructure is
> wired and ready; the commands below become operational once T02 is merged.

### Create a new migration (development)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/AI.DocumentIntelligence.Persistence \
  --startup-project src/AI.DocumentIntelligence.Api
```

### Apply migrations locally

```bash
# Against the local Postgres (via connection string in appsettings or user-secrets)
dotnet ef database update \
  --project src/AI.DocumentIntelligence.Persistence \
  --startup-project src/AI.DocumentIntelligence.Api
```

### Apply migrations via Docker Compose

The `migrate` service in `docker-compose.yml` runs migrations inside a container
against the compose-managed Postgres:

```bash
docker compose --profile migrate run --rm migrate
```

### Apply migrations in CI/CD

The CD pipeline (`cd.yml`) runs an automated migration step before deploying
new application containers. The step is conditional: it skips gracefully if no
migration files exist yet.

### pgvector extension

The first migration must enable the `vector` extension in Postgres:

```csharp
// In the DbContext OnModelCreating or a dedicated migration:
migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
```

The `pgvector/pgvector:pg17` image used in `docker-compose.yml` ships with the
extension pre-installed; only `CREATE EXTENSION` is needed.

---

## 6. Azure service setup

### 6.1 Azure Database for PostgreSQL — Flexible Server

```bash
az postgres flexible-server create \
  --resource-group <rg> \
  --name <server-name> \
  --location eastus \
  --admin-user <admin> \
  --admin-password <password> \
  --sku-name Standard_D4ds_v5 \
  --tier GeneralPurpose \
  --storage-size 128 \
  --version 17

# Enable pgvector extension
az postgres flexible-server parameter set \
  --resource-group <rg> \
  --server-name <server-name> \
  --name azure.extensions \
  --value vector
```

Set `DB_CONNECTION_STRING` in GitHub secrets (and Key Vault):

```
Host=<server-name>.postgres.database.azure.com;Port=5432;
Database=document_intelligence;Username=<admin>@<server-name>;
Password=<password>;Ssl Mode=Require;Trust Server Certificate=true;
```

### 6.2 Azure Blob Storage

```bash
az storage account create \
  --resource-group <rg> \
  --name <storage-account> \
  --sku Standard_LRS \
  --kind StorageV2 \
  --location eastus \
  --https-only true \
  --min-tls-version TLS1_2

az storage container create \
  --account-name <storage-account> \
  --name documents \
  --auth-mode login
```

Set `AZURE_STORAGE_CONNECTION` to the account connection string or use a managed
identity and set `AzureStorage__UseManagedIdentity=true` (requires code support).

### 6.3 Azure OpenAI (Microsoft Foundry)

1. Create an Azure OpenAI resource in the Azure portal or via CLI.
2. Deploy two models:
   - **Chat**: `gpt-4o` → deployment name e.g. `gpt-4o`
   - **Embeddings**: `text-embedding-3-small` → deployment name e.g. `text-embedding-3-small`
3. Copy the endpoint and API key into secrets.

Required environment variables:

```
AzureOpenAI__Endpoint=https://<resource>.openai.azure.com/
AzureOpenAI__ApiKey=<key>
AzureOpenAI__ChatDeployment=gpt-4o
AzureOpenAI__EmbeddingDeployment=text-embedding-3-small
AzureOpenAI__EmbeddingDimensions=1536
```

### 6.4 Azure AI Search

```bash
az search service create \
  --resource-group <rg> \
  --name <search-service> \
  --sku Standard \
  --location eastus \
  --partition-count 1 \
  --replica-count 1
```

Required environment variables:

```
AzureSearch__Endpoint=https://<search-service>.search.windows.net
AzureSearch__ApiKey=<admin-key>
AzureSearch__IndexName=document-chunks
AzureSearch__SemanticConfigurationName=document-chunks-semantic
AzureSearch__VectorDimensions=1536
```

The index schema is created automatically by the application on first startup
when no index exists (see `ISearchService.EnsureIndexAsync`).

---

## 7. CI/CD pipeline reference

### CI pipeline (`.github/workflows/ci.yml`)

Triggers on push/PR to `main`, `dev`, and `task/**` branches.

| Job | What it does |
|-----|-------------|
| `backend` | `dotnet restore` → `dotnet build` → `dotnet format --verify-no-changes` → `dotnet test` (with XPlat coverage) → ReportGenerator summary → coverage gate (≥60 %) |
| `frontend` | `npm ci` → `npm run build --configuration production` → `npm test --browsers=ChromeHeadless` |
| `docker-validate` | Builds both Docker images (no push) + validates both compose files |

### CD pipeline (`.github/workflows/cd.yml`)

Triggers on push to `main` or manual `workflow_dispatch`.

| Job | What it does |
|-----|-------------|
| `build-push` | Builds + pushes API and frontend images to GHCR with SHA, branch, and `latest` tags |
| `migrate` | Installs `dotnet-ef` → runs `dotnet ef database update` (skips if no migrations exist yet) |
| `deploy` | Placeholder — replace with your actual deployment target |

### GitHub Environments

Create two environments in **Settings → Environments**:

| Environment | Protection rules |
|-------------|-----------------|
| `staging` | No required reviewers; deploys automatically |
| `production` | Require at least one reviewer; restrict to `main` branch |

---

## 8. Production deployment

### Option A: Azure Container Apps (recommended)

```bash
# Create a Container Apps environment
az containerapp env create \
  --resource-group <rg> \
  --name doc-intelligence-env \
  --location eastus

# Deploy the API container app
az containerapp create \
  --resource-group <rg> \
  --environment doc-intelligence-env \
  --name api \
  --image ghcr.io/<owner>/ai-document-intelligence-api:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5 \
  --cpu 0.5 \
  --memory 1.0Gi \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    Jwt__Issuer=DocumentIntelligence \
    Jwt__Audience=DocumentIntelligenceClient \
    "Jwt__SecretKey=secretref:jwt-secret-key" \
    "ConnectionStrings__DefaultConnection=secretref:db-connection-string" \
    "AzureOpenAI__Endpoint=secretref:openai-endpoint" \
    "AzureOpenAI__ApiKey=secretref:openai-api-key" \
    "AzureSearch__Endpoint=secretref:search-endpoint" \
    "AzureSearch__ApiKey=secretref:search-api-key"

# Deploy the frontend container app
az containerapp create \
  --resource-group <rg> \
  --environment doc-intelligence-env \
  --name frontend \
  --image ghcr.io/<owner>/ai-document-intelligence-frontend:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 3 \
  --cpu 0.25 \
  --memory 0.5Gi
```

Update to a new image after CD pushes:

```bash
az containerapp update \
  --resource-group <rg> \
  --name api \
  --image ghcr.io/<owner>/ai-document-intelligence-api:sha-<short-sha>
```

### Option B: Docker Compose on a VM

```bash
# On the target VM (one-time setup):
git clone https://github.com/<org>/ai-document-intelligence.git /opt/app
cd /opt/app
cp .env.example .env
# Fill in all production values in .env

# Pull latest images and restart
docker compose pull
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Run migrations after pulling new images
docker compose --profile migrate run --rm migrate
```

For automatic deployments from CI, use SSH deploy action:

```yaml
- name: Deploy via SSH
  uses: appleboy/ssh-action@v1
  with:
    host: ${{ secrets.DEPLOY_HOST }}
    username: ${{ secrets.DEPLOY_USER }}
    key: ${{ secrets.DEPLOY_SSH_KEY }}
    script: |
      cd /opt/app
      docker compose pull
      docker compose --profile migrate run --rm migrate
      docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Option C: AKS (Kubernetes)

Create Kubernetes Secrets from Key Vault (using CSI driver), then apply manifests:

```bash
kubectl set image deployment/api \
  api=ghcr.io/<owner>/ai-document-intelligence-api@sha256:<digest>
kubectl set image deployment/frontend \
  frontend=ghcr.io/<owner>/ai-document-intelligence-frontend@sha256:<digest>
kubectl rollout status deployment/api
```

---

## 9. Health checks and observability

### Endpoints

| Endpoint | Purpose | Auth required |
|----------|---------|---------------|
| `GET /health` | All checks combined (DB, AI, Search) | No |
| `GET /health/live` | Liveness: process is up, no dependencies | No |
| `GET /health/ready` | Readiness: DB + Search + AI reachable | No |

### Response format

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "duration": "00:00:00.0123" },
    { "name": "azure-search", "status": "Healthy", "duration": "00:00:00.0456" }
  ],
  "totalDuration": "00:00:00.0580"
}
```

### Serilog structured logging

Logs are emitted as compact JSON to stdout (captured by Docker / ACA / AKS).
Set `Serilog__MinimumLevel__Default` to `Warning` in production to reduce volume.

### Application Insights

Set `ApplicationInsights__ConnectionString` to enable traces, metrics, and
live metrics. OpenTelemetry traces are exported to any OTLP-compatible backend
(Grafana, Jaeger, Zipkin) by setting:

```
OTEL_EXPORTER_OTLP_ENDPOINT=http://<collector>:4317
```

---

## 10. Scaling and performance

### API scaling

The API is stateless; scale horizontally by adding replicas.
Rate limiting (60 req/min per user, 10/min for auth endpoints) is enforced in-process;
for multi-replica deployments consider moving rate limiting to a gateway (APIM, Nginx).

### Database connection pooling

Npgsql uses a built-in connection pool. Adjust via the connection string:

```
Maximum Pool Size=50;Minimum Pool Size=5;Connection Idle Lifetime=300;
```

For large deployments (>10 API replicas), use PgBouncer in front of Postgres.

### Embedding and search performance

- Use `text-embedding-3-small` (1536 dim) for a balance of speed and accuracy.
- Azure AI Search Standard tier supports up to 1M documents per index; upgrade to
  Standard S2/S3 or Storage Optimized tiers for larger corpora.
- pgvector with `ivfflat` or `hnsw` index is used as a fallback when Azure AI Search
  is not configured; add the index once document count exceeds ~10,000.

### Frontend

nginx serves pre-compressed gzip responses. Static assets (JS/CSS/fonts) are
served with `Cache-Control: public, max-age=31536000, immutable` since Angular
appends content hashes to filenames in production builds.

---

## 11. Backup and disaster recovery

### PostgreSQL backup

**Azure Database for Flexible Server** takes automatic daily backups and supports
point-in-time restore (PITR) up to 35 days by default.

For manual / scripted backups:

```bash
# Dump (from inside the postgres container or with psql client)
pg_dump -h <host> -U <user> -d document_intelligence \
  --format=custom --compress=9 \
  -f backup-$(date +%Y%m%d-%H%M%S).dump

# Restore
pg_restore -h <host> -U <user> -d document_intelligence backup.dump
```

### Azure Blob Storage backup

Enable **Blob soft delete** (minimum 30 days) and **versioning** in the storage
account to protect against accidental deletion.

For cross-region redundancy, use **Zone-Redundant Storage (ZRS)** or
**Geo-Redundant Storage (GRS)**.

### Container image retention

GHCR retains tagged images indefinitely.  Use digest-based references in production
deployments (not `:latest`) so you can roll back to a specific build:

```bash
# Roll back API to a previous digest
az containerapp update --name api \
  --image ghcr.io/<owner>/ai-document-intelligence-api@sha256:<previous-digest>
```

---

## 12. Troubleshooting

### API fails to start: "Jwt:SecretKey is not configured"

The API enforces a non-empty, non-placeholder JWT secret at startup.
Set `Jwt__SecretKey` in your `.env` file or environment:

```bash
echo "Jwt__SecretKey=$(openssl rand -base64 48)" >> .env
```

### API container exits with code 1 immediately

Check the startup logs:

```bash
docker compose logs api
```

Common causes:
- Missing or malformed `ConnectionStrings__DefaultConnection`.
- Postgres healthcheck not yet passing — the `api` service waits for `condition: service_healthy`.

### `docker compose up` says "Service 'migrate' uses an undefined profile"

The migrate service is in the `migrate` profile and does **not** start by default.
Run it explicitly:

```bash
docker compose --profile migrate run --rm migrate
```

### Frontend shows a blank page / 404 on deep links

Ensure nginx is configured with `try_files $uri $uri/ /index.html`.
The provided `nginx.conf` already handles this.  If you are deploying behind a
reverse proxy, ensure it does not intercept non-file paths before they reach nginx.

### Karma tests fail in CI ("Chrome not found")

The CI pipeline uses `--browsers=ChromeHeadless` on `ubuntu-latest`, which
ships Chrome. If you see sandbox errors on a self-hosted runner inside Docker,
add `--no-sandbox` to Chrome flags via a custom `karma.conf.js`:

```javascript
customLaunchers: {
  ChromeHeadlessNoSandbox: {
    base: 'ChromeHeadless',
    flags: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage']
  }
}
```

Then update the test command: `--browsers=ChromeHeadlessNoSandbox`.

### EF Core migration fails: "Cannot open database"

The database must exist before running migrations.
On first deploy, create the database manually or via a script:

```bash
psql -h <host> -U <user> -c "CREATE DATABASE document_intelligence;"
```

Then run migrations.

---

*Last updated: 2026-07-03 (T17 — DevOps & Deployment)*
