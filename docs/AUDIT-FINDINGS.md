# Production-Readiness Audit — AI Document Intelligence Platform

**Date:** 2026-07-03
**Scope:** Full task-by-task / layer-by-layer review against `.claude/README.MD` (spec) and `CLAUDE.md` (standing rules).
> **REMEDIATION STATUS (2026-07-04): All P0 blockers and P1 majors below have been fixed.**
> Backend builds 0 warnings / **319 tests pass** (incl. real pgvector + Azurite Testcontainers
> integration tests); frontend builds and **113 specs pass** with all mock-data fallbacks removed.
> The EF Core migration was verified applying against a real `pgvector/pgvector:pg17` database.
> See `docs/DEPLOYMENT.md`. Remaining items are documented as known limitations there
> (multi-session refresh tokens; server-side combined-page limit; full aggregation pushdown for admin metrics).
> The findings below are preserved as the original "before" audit.

**Verdict (original):** **Not production-ready.** The solution *builds clean (0 warnings)* and *all 277 backend tests + frontend build pass*, but that green status is misleading: the persistence layer that everything depends on was never implemented, so **no write path works at runtime** and two headline features are missing/faked.

---

## How to read this

Each finding lists: **severity**, the **task** it maps to, the **evidence** (file), and the **impact**. Severities:

- **P0 — Blocker:** app cannot function in production as-is.
- **P1 — Major:** a required capability is missing or faked.
- **P2 — Should-fix:** correctness/quality gap for enterprise grade.

The root cause of most P0/P1 issues is a single unfinished task — **T02 (Persistence)** — that was marked "Not started" in the index yet had six downstream tasks marked "Done" built on top of it.

---

## P0 — Blockers

### 1. The entire Persistence layer (T02) is a stub — every DB write throws at runtime
- **Task:** T02 (marked *Not started* in `INDEX.md`, yet T03/T05/T06/T07/T08/T14 depend on it and are *Done*).
- **Evidence:**
  - `src/AI.DocumentIntelligence.Persistence/Repositories/UserRepository.cs`, `DocumentRepository.cs`, `UnitOfWork.cs` — **every method throws `NotImplementedException("… pending T02")`**.
  - **No `DbContext` exists** anywhere in the solution (grep for `DbContext` finds only the interfaces and the audit middleware).
  - **No entity configurations**, **no EF Core migrations** (`find -iname '*migration*'` → nothing).
  - `DependencyInjection.cs` registers the stubs into the real DI container.
- **Impact:** Registration, login/refresh-token storage, document upload, usage tracking, audit logging — **all call `IUnitOfWork.SaveChangesAsync()` / a repository and throw `NotImplementedException` the moment a real request hits them.** The API starts and serves Swagger, but any endpoint that persists returns a 500. This is the single most important gap.
- **Fix:** Implement `AppDbContext` with `IEntityTypeConfiguration<>` for all 9 entities, real generic `Repository<T>` + `UnitOfWork` over EF Core, register `AddDbContext` with Npgsql + pgvector, and generate the initial migration.

### 2. `pgvector` / PostgreSQL is referenced but never actually used
- **Task:** T02 / T05 (RAG).
- **Evidence:** `Pgvector`, `Pgvector.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL` are in `Directory.Packages.props` and the Persistence `.csproj`, but with no `DbContext` there is **no vector column mapping and no vector query**. `DocumentChunk` (with an embedding) is a domain entity that is **never persisted** — `IngestDocumentCommandHandler` pushes chunks straight to the search index and never saves them.
- **Impact:** The "PostgreSQL + pgvector" pillar of the spec is unfulfilled. The DB stores nothing.

### 3. No pgvector search fallback — RAG retrieval requires a live Azure AI Search account
- **Task:** T05 / T07.
- **Evidence:** Only `AzureSearchService` implements `ISearchService` (`Infrastructure/DependencyInjection.cs:71` registers it as the sole implementation). `docker-compose.yml` comments claim *"pgvector fallback is used"* locally, but **no such implementation exists.**
- **Impact:** With blank Azure Search config (the documented local setup), retrieval fails. The system cannot be demoed or run end-to-end without a paid Azure AI Search resource, contradicting the compose story.

### 4. Analysis feature (T10) — the headline capability — is missing from the frontend
- **Task:** T10 (*Not started*), but it's one of the two primary product features and its route is live.
- **Evidence:** `app.routes.ts` maps `/analysis` to `features/placeholder/analysis-placeholder`. There is **no analysis feature folder, no upload-for-analysis UI, no analysis-results view, no analysis API service.** The only `document-upload` component lives inside the *comparison* feature.
- **Impact:** "Analyze documents and extract insights" — the flagship card on the landing page — leads to a placeholder. Half the product is not built on the UI side.

---

## P1 — Major

### 5. Sessions, audit logs, and usage metrics are never persisted
- **Task:** T03/T07/T08/T15 (persistence requirement in spec lines 398–423).
- **Evidence:** Entities `AnalysisSession`, `ComparisonSession`, `ChatSession`, `ChatMessage`, `AuditLog`, `AiUsageMetric` all exist in `Domain/Entities`, but:
  - `AnalyzeDocumentsCommandHandler` / `CompareDocumentsCommandHandler` just return the AI result — they **never create a session row**.
  - `AnalysisService.TrackUsageAsync(...)` writes `AiUsageMetric` via the stubbed `IUnitOfWork` → throws at runtime (and only runs when a user is authenticated).
- **Impact:** No analysis/comparison/chat history, no audit trail, no usage/cost accounting — even though the spec explicitly requires all of these. "Audit & Reporting" is unattainable.

### 6. Admin dashboard shows fabricated data
- **Task:** T13 (*Done*).
- **Evidence:** `AdminController` exposes only user CRUD — **there is no `/admin/metrics` endpoint.** The frontend `admin-api.service.ts` calls `${apiBase}/admin/metrics`, and on the (guaranteed) error **falls back to a large block of hardcoded `buildMockMetrics()` data** (`totalUsers: 24`, mock activity feed, etc.).
- **Impact:** Total Users/Documents/Analyses/Comparisons, AI usage, cost, avg processing time, recent activity — all required by spec — are invented numbers, not real metrics. No backend aggregation query exists to produce them.

### 7. Test suite gives false confidence
- **Task:** T16 (*Done*).
- **Evidence:** 277 tests pass, but the "integration" tests substitute `Integration/Fakes/InMemoryUnitOfWork` and in-memory repositories. There is **no test that exercises the real EF Core `DbContext`/repositories** (because they don't exist). The T16 brief claims Testcontainers-based DB integration; those tests cannot be validating the real persistence path.
- **Impact:** Green CI masks the #1 blocker. A production build would pass every gate and then 500 on first write.

### 8. File storage is local-disk only; configured Azurite blob is unused
- **Task:** T04 / T17.
- **Evidence:** Only `LocalFileStorage` implements `IFileStorage`. `docker-compose.yml` provisions Azurite and injects `ConnectionStrings__BlobStorage`, but **no blob-storage implementation consumes it.** Uploaded files land on the container's ephemeral filesystem.
- **Impact:** Uploads are lost on container restart and don't scale across replicas — not production-grade. The Azurite service is dead config.

### 9. Single refresh token per user (no multi-session), and it can't persist anyway
- **Task:** T06.
- **Evidence:** `User` holds a single `RefreshTokenHash` / `RefreshTokenExpiresAtUtc`. Login on a second device overwrites the first; there's no refresh-token table/rotation history. And the persistence path runs through the stubbed repo (#1).
- **Impact:** Logging in anywhere silently revokes every other session; refresh/logout throw at runtime today.

---

## P2 — Should-fix

### 10. `INDEX.md` status is unreliable
- T00 and T02 are marked *Not started* despite foundation/config existing and being depended upon; several tasks are "Done" on feature branches (`dev`, `task/T1x-*`) that aren't merged to `main`. The tracker doesn't reflect reality, which is how the T02 gap went unnoticed.

### 11. Production deployment documentation is missing (a T17 deliverable)
- `docs/` contains only `observability-dashboard.md`. The spec's "Production Deployment Documentation" final deliverable is not present (README covers local dev only).

### 12. Auth rate-limit policy defined but not verifiably applied
- Program.cs defines a tighter `"AuthEndpoints"` limiter, but confirm `AuthController` actually carries `[EnableRateLimiting("AuthEndpoints")]`; otherwise only the 60/min global limiter protects login/register (brute-force surface).

### 13. Upload page-count / combined-limit rules
- Spec caps combined pages at 500 and combined size configurable across up to 4 docs. `UploadDocumentCommandHandler` validates one file at a time; verify the *combined* multi-document constraints are enforced somewhere (per-request, not per-file).

---

## What is genuinely solid (so it's not all bad)

- **Clean Architecture boundaries, CQRS/MediatR, Result pattern, FluentValidation, AutoMapper** are consistently applied across the Application layer.
- **AI provider layer is real:** `AzureOpenAiProvider` (default) plus OpenAI/Anthropic/Ollama behind `IAIProvider`, with cost/token telemetry.
- **API layer is strong:** API versioning, JWT + role policies, global exception handler, per-user rate limiting, Serilog request logging, correlation IDs, health checks (`/health`, `/live`, `/ready`), OpenTelemetry.
- **Export** (PDF/Word/Excel/Markdown) formatters are implemented for real.
- **Docker/Compose/CI/CD** are well-structured (multi-stage Dockerfile, prod overlay, GitHub Actions build+test) — they even *correctly anticipate* the T02 gap and skip migrations gracefully.
- **Citations** are modeled and flow through the AI response contracts.

---

## Recommended order to reach production-ready

1. **T02 first** — `DbContext`, entity configs, `Repository<T>`/`UnitOfWork`, pgvector mapping, initial migration. Unblocks #1, #2, #5, #7, #9.
2. **Persist sessions + usage + audit** in the analysis/comparison/chat handlers (#5).
3. **pgvector search fallback** implementing `ISearchService` so RAG runs without Azure (#3).
4. **Build the Analysis frontend feature** (#4) and a real **`/admin/metrics`** query + endpoint (#6).
5. **Blob storage implementation** for uploads (#8), then **replace in-memory fakes with Testcontainers** integration tests over the real schema (#7).
6. Reconcile `INDEX.md`, add deployment docs, verify auth rate-limit + combined-upload limits.
