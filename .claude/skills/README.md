# Project Skills

This folder contains project-specific skills for the AI Document Intelligence Platform.

Use these skills as reusable guidance blocks when implementing tasks from [.claude/tasks/INDEX.md](../tasks/INDEX.md).

## Skills

1. `task-orchestration`
- Purpose: Standard task workflow (pick task, implement, validate, architecture review, mark done).
- File: [task-orchestration/SKILL.md](task-orchestration/SKILL.md)

2. `backend-cqrs-clean-architecture`
- Purpose: .NET 10 backend guardrails for Clean Architecture, CQRS/MediatR, Result pattern, validation, and mapping.
- File: [backend-cqrs-clean-architecture/SKILL.md](backend-cqrs-clean-architecture/SKILL.md)

3. `rag-citations`
- Purpose: RAG/AI output quality and mandatory citation structure (document, page, paragraph, confidence).
- File: [rag-citations/SKILL.md](rag-citations/SKILL.md)

4. `frontend-angular-standalone-signals`
- Purpose: Angular 20 implementation patterns (standalone components, signals, strict typing, Material + Tailwind).
- File: [frontend-angular-standalone-signals/SKILL.md](frontend-angular-standalone-signals/SKILL.md)

5. `quality-gates`
- Purpose: Build/test/verification checklist for backend and UI before considering work done.
- File: [quality-gates/SKILL.md](quality-gates/SKILL.md)

## Notes

- These skills complement existing agent role files in [.claude/agents](../agents).
- If needed later, you can add command wrappers in [.claude/commands](../commands) that call workflows described here.
