---
name: backend-cqrs-clean-architecture
description: "Backend implementation guardrails for .NET 10 in this repo: Clean Architecture boundaries, CQRS/MediatR, Result pattern, Repository + Unit of Work, FluentValidation, AutoMapper, API versioning, and middleware conventions. USE FOR: backend feature work, API endpoints, handlers, validators, mappings."
---

# Backend CQRS and Clean Architecture Skill

Use this skill for any changes under `src/AI.DocumentIntelligence.*` backend projects.

## Non-negotiable architecture rules

1. Dependency direction: `Api -> Application -> Domain`.
2. `Infrastructure` and `Persistence` implement abstractions from inner layers.
3. `Domain` must not depend on outer layers.
4. CQRS + MediatR: one command/query + handler per use case.
5. Controllers remain thin; business logic lives in handlers/domain services.

## Patterns to enforce

- Expected failures return `Result`/`Result<T>`.
- Exceptions are for truly exceptional paths handled by global middleware.
- Use repositories + unit of work; no direct `DbContext` usage outside Persistence.
- Validate inputs via FluentValidation pipeline behavior.
- Use AutoMapper for entity <-> DTO transformations.
- Keep API versioning and authorization consistent with existing controllers.

## Code style

- Nullable enabled.
- File-scoped namespaces.
- One public type per file.
- Async method names end with `Async`.
- Prefer immutable DTO records.

## Verification

Run after backend edits:

1. `dotnet build`
2. `dotnet test`
3. `dotnet format --verify-no-changes` (when available in workflow)
