---
name: quality-gates
description: "Cross-cutting quality gates for this repository before marking work complete. USE FOR: pre-merge validation, task completion checks, and release confidence checks."
---

# Quality Gates Skill

Use this skill before declaring work complete.

## Required checks by change type

### Backend changes

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`

### Frontend changes

1. `npm ci`
2. `npm run build`
3. `npm test -- --watch=false --browsers=ChromeHeadless`

### Full-stack or shared contract changes

Run both backend and frontend checks.

## Architecture and correctness gates

- Confirm changed code respects Clean Architecture boundaries.
- Confirm CQRS handlers/validators are in place for new backend use cases.
- Confirm API contracts remain versioned and backward-safe unless task allows breakage.
- Confirm AI responses still satisfy citation requirements.

## Final workflow gate

1. Run `architecture-reviewer` on the diff.
2. Update task status in `.claude/tasks/INDEX.md` only after all checks pass.
3. If any check fails, report failures and do not claim completion.
