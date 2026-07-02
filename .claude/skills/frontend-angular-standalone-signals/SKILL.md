---
name: frontend-angular-standalone-signals
description: "Angular 20 frontend implementation patterns for this repo: standalone components, signal-based state, strict typing, Angular Material + Tailwind, responsive dark UI, and route-level feature organization. USE FOR: UI feature work in the Angular app."
---

# Frontend Angular Standalone Signals Skill

Use this skill for changes under `ui/AI.DocumentIntelligence.UI/src`.

## Core frontend rules

1. Use standalone components.
2. Use signal-based state where practical.
3. Keep TypeScript strict; avoid `any`.
4. Prefer typed reactive forms for form-heavy screens.
5. Use `ChangeDetectionStrategy.OnPush` by default.

## UI and architecture conventions

- Follow feature-folder organization.
- Keep presentation and data-access concerns separated.
- Reuse shared UI primitives and Material components consistently.
- Preserve existing dark-theme visual language unless task explicitly changes design.
- Ensure mobile and desktop responsiveness.

## API integration guidance

- Keep API models strongly typed.
- Normalize server responses before binding to UI when needed.
- Handle loading, success, and error states explicitly.
- Surface citation data from API in analysis/comparison/chat views.

## Verification

Run after frontend edits:

1. `npm ci` (if dependencies need refresh)
2. `npm run build`
3. `npm test -- --watch=false --browsers=ChromeHeadless`
