# AI Document Intelligence Platform

Enterprise-grade document analysis, comparison, and AI-powered chat, built on
Retrieval-Augmented Generation (RAG).

This repository is built incrementally, task by task — see
[`.claude/tasks/INDEX.md`](.claude/tasks/INDEX.md) for the build plan and
[`.claude/README.MD`](.claude/README.MD) for the full product/technical spec.
Standing conventions and architecture rules live in [`CLAUDE.md`](CLAUDE.md).

## Tech stack

| Layer          | Technology |
|----------------|------------|
| Frontend       | Angular 20, standalone components, Signals, Angular Material, Tailwind CSS, dark theme |
| Backend        | .NET 10 Web API, Clean Architecture, CQRS/MediatR, FluentValidation, AutoMapper |
| Database       | PostgreSQL + `pgvector` |
| AI & Search    | Azure OpenAI (Foundry) + Azure AI Search, behind a provider-agnostic `IAIProvider` |
| Observability  | Serilog, Application Insights, OpenTelemetry |
| DevOps         | Docker, Docker Compose, GitHub Actions |

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

Dependencies flow inward only: `Api → Application → Domain`. `Infrastructure`
and `Persistence` implement interfaces declared in `Application`/`Domain`;
`Domain` depends on nothing.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org) and npm
- [Docker](https://www.docker.com/) with Docker Compose v2

## Getting started

```bash
# Backend
dotnet restore
dotnet build

# Frontend
cd frontend
npm ci
npm run build
cd ..

# Local stack (Postgres + pgvector, Azurite, Api, frontend)
cp .env.example .env
docker compose up -d
```

Once the stack is up:
- API health check: `http://localhost:8080/health`
- API Swagger UI (Development): `http://localhost:8080/swagger`
- Frontend: `http://localhost:4200`

See [`CLAUDE.md`](CLAUDE.md) for the full command reference and
[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) for CI/CD and deployment details.

## Repository conventions

- Clean Architecture, CQRS/MediatR, Repository + Unit of Work, Result pattern
  (no exceptions for control flow), API versioning, global exception
  middleware — see [`CLAUDE.md`](CLAUDE.md) for the full rule set.
- Every AI response (analysis, comparison, chat) carries citations: document
  name, page number, paragraph reference, and confidence score.
- Never commit secrets. Local configuration goes through `.env` (gitignored,
  see `.env.example`), `dotnet user-secrets`, or environment variables; in
  Azure, through Key Vault.
