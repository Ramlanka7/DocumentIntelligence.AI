# Task Index — AI Document Intelligence Platform

This is the master tracker. Build the platform by working tasks **in dependency order**.

## How to work a task
1. Pick the lowest-numbered task that is `Not started` and whose dependencies are all `Done`.
2. Read its task file in full — each references the exact spec line ranges in
   [../README.MD](../README.MD).
3. Delegate the work to the task's **Suggested agent** (see [../agents/](../agents/)).
4. Implement until **every acceptance criterion** passes; build/tests must be green.
5. Run the `architecture-reviewer` agent over the diff.
6. Set the task's **Status** below to `Done` and note the branch.

Status values: `Not started` · `In progress` · `Blocked` · `Done`

## Tasks
| ID  | Title                         | Depends on        | Suggested agent              | Status      |
|-----|-------------------------------|-------------------|------------------------------|-------------|
| T00 | Foundation & repo setup       | —                 | devops-engineer              | Not started |
| T01 | Domain layer                  | T00               | dotnet-backend-engineer      | Done        |
| T02 | Persistence (EF Core+pgvector)| T00, T01          | dotnet-backend-engineer      | Not started |
| T03 | Application core (CQRS)       | T01, T02          | dotnet-backend-engineer      | Done        |
| T04 | Document processing layer     | T03               | rag-ai-engineer              | Not started |
| T05 | RAG pipeline                  | T03, T04          | rag-ai-engineer              | Not started |
| T06 | Auth & security               | T02, T03          | dotnet-backend-engineer      | Not started |
| T07 | AI service layer              | T03, T05          | rag-ai-engineer              | Not started |
| T08 | API layer                     | T03, T06, T07     | dotnet-backend-engineer      | Not started |
| T09 | Frontend foundation           | T00               | angular-frontend-engineer    | Not started |
| T10 | Analysis feature              | T08, T09          | angular-frontend-engineer    | Not started |
| T11 | Comparison feature            | T08, T09          | angular-frontend-engineer    | Not started |
| T12 | Chat feature                  | T08, T09          | angular-frontend-engineer    | Not started |
| T13 | Admin dashboard               | T08, T09          | angular-frontend-engineer    | Not started |
| T14 | Export features               | T08, T10, T11     | dotnet-backend-engineer      | Not started |
| T15 | Observability                 | T08               | dotnet-backend-engineer      | Not started |
| T16 | Testing                       | T08–T14           | test-engineer                | Not started |
| T17 | DevOps & deployment           | T00, T08, T09     | devops-engineer              | Not started |

## Milestones
- **M1 — Backend foundation**: T00–T03 (compiles, DB migrates).
- **M2 — Intelligence core**: T04–T08 (RAG + AI + API working end-to-end via Swagger).
- **M3 — Product UI**: T09–T13 (full Angular app against the API).
- **M4 — Hardening & ship**: T14–T17 (export, observability, tests, CI/CD, deploy docs).
