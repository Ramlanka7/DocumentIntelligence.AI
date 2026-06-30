# CLAUDE.md — Standing rules for the AI Document Intelligence Platform

These rules apply to **every** edit in this repo and are loaded automatically each
session — you never need to restate them in a prompt.

- **Requirements / product spec** (source of truth): [.claude/README.MD](.claude/README.MD).
- **Task briefs** (this repo is built incrementally): [.claude/tasks/INDEX.md](.claude/tasks/INDEX.md).
  Read the relevant task file in full before working.

## Tech stack (standing facts)
- **Frontend**: Angular 20, standalone components, Signals, Angular Material, Tailwind, TypeScript (strict), dark theme.
- **Backend**: .NET 10 Web API, Clean Architecture, CQRS/MediatR, FluentValidation, AutoMapper, Repository + Unit of Work.
- **Database**: PostgreSQL + `pgvector`.
- **AI & search**: Azure OpenAI (Foundry) + Azure AI Search. Provider abstracted via `IAIProvider`;
  **Azure OpenAI is the default**, with Anthropic Claude / OpenAI / Ollama behind the same interface.
- **Observability**: Serilog, Application Insights, OpenTelemetry. **DevOps**: Docker, Docker Compose, GitHub Actions.

## Architecture rules (non-negotiable)
- **Clean Architecture dependency direction**: `Api → Application → Domain`. `Infrastructure`
  and `Persistence` implement interfaces declared in `Application`/`Domain`. **`Domain`
  depends on nothing.** Never reference outer layers from inner ones.
- **CQRS + MediatR**: every use case is a Command or Query with its own handler. No business
  logic in controllers.
- **Result pattern**: return `Result`/`Result<T>` for expected failures. Do **not** throw
  exceptions for control flow; exceptions are for the global exception middleware only.
- **Repository + Unit of Work** for all data access; no `DbContext` use outside Persistence.
- **FluentValidation** for input (via a MediatR validation behavior). **AutoMapper** for entity↔DTO mapping.
- **API versioning** on all endpoints; **global exception handling middleware** for unhandled errors.
- Design every abstraction for **extensibility** — new AI providers, document types, and
  workflows must drop in without modifying existing code (Open/Closed).

## Solution layout
```
src/
  AI.DocumentIntelligence.Api            # Web API, controllers, middleware, DI composition
  AI.DocumentIntelligence.Application    # CQRS, handlers, interfaces, validators, mappings
  AI.DocumentIntelligence.Domain         # Entities, value objects, enums, domain errors
  AI.DocumentIntelligence.Infrastructure # AI providers, Azure AI Search, doc processors, auth
  AI.DocumentIntelligence.Persistence    # EF Core DbContext, configs, repositories, migrations
  AI.DocumentIntelligence.Tests          # Unit + integration tests
frontend/                                # Angular 20 workspace
```

## Conventions
- **C#**: nullable enabled, file-scoped namespaces, one public type per file, `Async` suffix on
  async methods, `private readonly` fields, records for DTOs/value objects. Treat warnings as errors where practical.
- **Angular**: standalone components, `ChangeDetectionStrategy.OnPush`, signal-based state,
  typed reactive forms, no `any`, feature-folder structure, `kebab-case` filenames.
- **Citations (hard rule)**: every AI analysis/comparison/chat response MUST carry citations —
  document name, page number, paragraph reference, and confidence score.
- **Commits**: small, scoped, one task per branch (`task/T0X-short-name`), Conventional-style
  messages. Never commit secrets; use config/user-secrets/env.

## Specialist roles
For role-specific work, delegate to the project subagents instead of re-describing the role:
- **dotnet-backend-engineer** — .NET backend: Clean Architecture, CQRS/MediatR, EF Core, auth, API (T01–T03, T06, T08, T14, T15).
- **rag-ai-engineer** — document processing, RAG pipeline, AI service layer, embeddings, Azure AI Search, `IAIProvider`, citations (T04, T05, T07).
- **angular-frontend-engineer** — Angular 20 UI: components, signals, Material, Tailwind, dark theme (T09–T13).
- **devops-engineer** — repo/solution setup, Docker, Compose, GitHub Actions CI/CD (T00, T17).
- **test-engineer** — unit / integration (Testcontainers) / frontend tests; keeping the suite green (T16, and verifying any task).
- **architecture-reviewer** — read-only Clean Architecture / SOLID / CQRS / convention review; run at the end of each task.

## Definition of done (before you call a change "done")
- The affected project **builds**, and its **tests pass** (`dotnet test` / `frontend` `npm test`).
- Every acceptance criterion in the task brief is satisfied; AI responses carry citations.
- Run **architecture-reviewer** on the diff.
- Update the task's status to `Done` in [.claude/tasks/INDEX.md](.claude/tasks/INDEX.md).
- Report failures honestly with the output — never claim green while red.

## Commands

### Backend (repo root, `AI.DocumentIntelligence.sln`)
- Restore: `dotnet restore`
- Build (zero warnings/errors, `TreatWarningsAsErrors` is on): `dotnet build`
- Test: `dotnet test`
- Format / style check: `dotnet format` · `dotnet format --verify-no-changes` (CI)
- Run the API directly: `dotnet run --project src/AI.DocumentIntelligence.Api`
- EF Core migrations (once `Persistence` has a `DbContext`):
  `dotnet ef migrations add <Name> --project src/AI.DocumentIntelligence.Persistence --startup-project src/AI.DocumentIntelligence.Api`
  `dotnet ef database update --project src/AI.DocumentIntelligence.Persistence --startup-project src/AI.DocumentIntelligence.Api`

### Frontend (in `frontend/`)
- Install (reproducible, CI-equivalent): `npm ci`
- Build: `npm run build`
- Test (headless): `npm test -- --watch=false --browsers=ChromeHeadless`
- Serve locally: `npm start` (http://localhost:4200)

### Local stack (Docker Compose)
- Copy env template once: `cp .env.example .env`
- Start everything (Postgres+pgvector, Azurite, Api, frontend): `docker compose up -d`
- Validate compose files without starting anything: `docker compose config`
- Production-shaped overlay: `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d`
- Tear down: `docker compose down` (add `-v` to also drop the Postgres/Azurite volumes)
- Endpoints once up: API `http://localhost:8080/health`, Swagger `http://localhost:8080/swagger`, frontend `http://localhost:4200`
