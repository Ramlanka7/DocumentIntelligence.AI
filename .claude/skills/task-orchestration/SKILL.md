---
name: task-orchestration
description: "Task execution workflow for this repo: choose next task by dependency order, implement with the correct specialist agent, validate acceptance criteria, run architecture review, and update task status. USE FOR: task kickoff, task sequencing, done checklist, status updates."
---

# Task Orchestration Skill

Use this skill whenever you are asked to implement or continue a task from `.claude/tasks`.

## Source of truth

1. Root repo rules: `CLAUDE.md`.
2. Product spec: `.claude/README.MD`.
3. Task tracker: `.claude/tasks/INDEX.md`.
4. Task brief: `.claude/tasks/T0X-*.md` for the selected task.

## Execution flow

1. Select the lowest-numbered task in `Not started` state whose dependencies are all `Done`.
2. Read the selected task brief fully before coding.
3. Use the suggested role agent from `.claude/agents` for implementation.
4. Implement only the scope required by the task acceptance criteria.
5. Validate with build/tests relevant to changed areas.
6. Run architecture review using `architecture-reviewer`.
7. Update `.claude/tasks/INDEX.md` status only when the task is verifiably complete.

## Guardrails

- Do not mark a task `Done` if build/tests are red.
- Do not skip architecture review for substantial code changes.
- Keep changes scoped; avoid unrelated refactors.
- Report blockers and partial completion honestly.

## Minimum done checklist

- Acceptance criteria passed.
- Build successful for affected projects.
- Tests passed for affected projects.
- Architecture review completed.
- Task index updated.
