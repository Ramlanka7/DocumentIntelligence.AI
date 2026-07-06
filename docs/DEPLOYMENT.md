# Production Deployment Guide — AI Document Intelligence Platform

This guide covers running the platform locally, configuring it for production, applying the
database schema, and the CI/CD pipeline. It reflects the actual repository state (verified: the
backend builds with 0 warnings, 319 tests pass including real pgvector + Azurite Testcontainers
integration tests; the frontend builds and 113 specs pass).

## Architecture at a glance

| Component | Tech | Container |
|-----------|------|-----------|
| Frontend | Angular 20 (nginx) | `ui/AI.DocumentIntelligence.UI/Dockerfile` |
| API | .NET 10 Web API | root `Dockerfile` (multi-stage: `build` → `final`, plus `migrator`) |
| Database | PostgreSQL 17 + pgvector | `pgvector/pgvector:pg17` |
| Blob storage | Azure Blob (Azurite locally) | `mcr.microsoft.com/azure-storage/azurite` |
| Vector search | pgvector (default) or Azure AI Search | in-process |
| AI | Azure OpenAI (default `IAIProvider`); OpenAI / Anthropic / Ollama pluggable | external |

**Search provider selection is automatic:** when `AzureSearch:Endpoint` is blank the app uses the
in-database **pgvector** search service (`PgVectorSearchService`); set the Azure AI Search endpoint +
key to switch to Azure AI Search — no code change (Open/Closed).

**Storage provider selection is automatic:** when `AzureStorage:ConnectionString` is non-empty the app
uses `AzureBlobFileStorage`; otherwise it falls back to `LocalFileStorage` (dev only — ephemeral disk).

## 1. Local stack (Docker Compose)

```bash
cp .env.example .env                 # fill in secrets (see §3)
docker compose up -d                 # Postgres+pgvector, Azurite, API, frontend
docker compose --profile migrate run --rm migrate   # apply EF Core migrations
```

Endpoints once up:
- API: `http://localhost:8080/health` · Swagger: `http://localhost:8080/swagger`
- Frontend: `http://localhost:4200`

Validate compose without starting: `docker compose config`. Tear down: `docker compose down`
(add `-v` to drop the Postgres/Azurite volumes).

Production-shaped overlay (sets `ASPNETCORE_ENVIRONMENT=Production`, disables Swagger, etc.):
```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 2. Database schema / migrations

The schema is created by EF Core migrations (initial migration `InitialCreate` under
`src/AI.DocumentIntelligence.Persistence/Migrations/`). It enables the `vector` extension and creates
all 10 tables, including `document_chunks.embedding vector(1536)` with an HNSW cosine index.

- Via compose: `docker compose --profile migrate run --rm migrate`
- Via CLI:
  ```bash
  dotnet ef database update \
    --project src/AI.DocumentIntelligence.Persistence \
    --startup-project src/AI.DocumentIntelligence.Api
  ```
  (requires `ConnectionStrings__DefaultConnection` and `Jwt__SecretKey` in the environment).

The CD pipeline runs migrations automatically before deploying (`.github/workflows/cd.yml`, `migrate` job).

## 3. First-run bootstrap (initial admin user)

Registration (`POST /auth/register`) requires an existing **Admin**, so a fresh database
must be seeded with one before anyone can log in:

1. Set `Database__SeedOnStartup=true` and provide `Seed__AdminPassword` (and optionally
   `Seed__AdminEmail`, default `admin@documentintelligence.local`) via your secret store.
2. Start the API after migrations have been applied (locally, `Database__AutoMigrate=true`
   handles this; docker-compose enables both by default — just set `Seed__AdminPassword`
   in `.env`).
3. Log in as the seeded admin and create real users via `POST /auth/register`; then
   consider disabling `Database__SeedOnStartup`.

The seeder is idempotent (a no-op once the admin exists) and stores only a BCrypt hash.
In **Production**, enabling seeding without `Seed__AdminPassword` fails startup rather
than falling back to any default password. Seeding is the only bootstrap path — there is
no hardcoded account.

## 4. Configuration & secrets

Configuration binds from environment variables (double-underscore = nested key). **Never commit real
secrets** — use user-secrets locally and your platform's secret store (Key Vault, GitHub Actions
secrets, etc.) in CI/CD.

| Key | Required | Notes |
|-----|----------|-------|
| `Jwt__SecretKey` | **Yes** | ≥ 256-bit. App refuses to start with the placeholder. |
| `Jwt__Issuer`, `Jwt__Audience` | Yes | Token validation. |
| `ConnectionStrings__DefaultConnection` | **Yes** | Npgsql connection string to Postgres+pgvector. |
| `AzureOpenAI__Endpoint`, `AzureOpenAI__ApiKey`, `AzureOpenAI__ChatDeployment`, `AzureOpenAI__EmbeddingDeployment` | Yes (for real AI) | Default provider. Leave blank to run without live AI (calls will fail gracefully). |
| `AzureStorage__ConnectionString`, `AzureStorage__ContainerName` | Prod | Non-empty activates durable blob storage. |
| `AzureSearch__Endpoint`, `AzureSearch__ApiKey`, `AzureSearch__IndexName` | Optional | Blank → pgvector search is used. |
| `Ai__ProviderName` | Optional | Switch `IAIProvider` (AzureOpenAI / OpenAI / Anthropic / Ollama). |
| `ApplicationInsights__ConnectionString` / OTLP exporter vars | Optional | Observability exporters activate only when configured. |

## 5. Health, observability, security

- **Health:** `/health` (full), `/health/live` (liveness, no deps), `/health/ready` (readiness — DB,
  search, AI). Wire your orchestrator's liveness/readiness probes to `/health/live` and `/health/ready`.
- **Observability:** Serilog structured logs with correlation IDs; OpenTelemetry traces + metrics
  (OTLP and Azure Monitor exporters activate when configured). See `docs/observability-dashboard.md`.
- **Security:** JWT bearer + refresh-token rotation (tokens hashed at rest); role policies
  (Admin/Analyst/Viewer); per-user rate limiting (60/min global, 10/min on auth endpoints); audit
  logging; file-type/size validation on upload; CORS restricted to the configured origin.

## 6. CI/CD (GitHub Actions)

- **`ci.yml`** (on PR/push): `dotnet format --verify-no-changes`, backend build + `dotnet test`,
  frontend `npm ci` + build + headless tests, and a no-push Docker image build.
  > Note: the integration tests use Testcontainers (pgvector + Azurite), so the CI runner must have
  > Docker available (GitHub-hosted Linux runners do).
- **`cd.yml`** (on release/main): builds and pushes API + frontend images to GHCR, runs EF Core
  migrations against the target database, then deploys.

## 7. Production checklist

- [ ] Strong `Jwt__SecretKey` and all secrets sourced from a secret store (not `.env`).
- [ ] `ASPNETCORE_ENVIRONMENT=Production` (Swagger disabled; enforced by the prod overlay).
- [ ] Managed Postgres with the `vector` extension available; migrations applied.
- [ ] `AzureStorage__ConnectionString` points at real Blob Storage (uploads must be durable).
- [ ] Real `AzureOpenAI` (or chosen provider) credentials configured.
- [ ] TLS terminated at the ingress; `/health/*` wired to orchestrator probes.
- [ ] Log sink + OpenTelemetry/App Insights exporter configured; alerts on error rate & AI cost.

## Known limitations / future hardening

- **Refresh tokens are single-session per user** (one active refresh token; logging in elsewhere
  revokes the previous session). A `RefreshToken` table for multi-device sessions is a future enhancement.
  Refresh tokens are delivered to browsers exclusively as an `HttpOnly; Secure; SameSite=Strict`
  cookie scoped to `/api/v1/auth` — they never appear in response bodies or Web Storage.
- **Document ingestion is asynchronous and in-process** (bounded channel + background worker).
  Jobs do not survive an unclean shutdown; on restart, documents stranded in `Processing` are
  marked `Failed` with a re-upload message. Swap `ChannelIngestionScheduler` for a durable queue
  (Azure Storage Queue / Service Bus) behind `IIngestionScheduler` for multi-instance ingestion.
- **Rate limiting is per-instance** (in-process fixed window). With multiple replicas the
  effective limit is N × the configured value; move to a gateway or Redis-backed limiter when
  scaling out matters.
- **Combined-page limit (500 pages across a session)** is not yet enforced server-side; the UI caps
  uploads at 4 documents. Add a session-level page-count guard for strict enforcement.
- **API integration tests use in-memory fakes** for speed; real-DB coverage is provided by the
  pgvector/Azurite Testcontainers tests. Converting the full API suite to Testcontainers is optional.
