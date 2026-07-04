---
name: rag-citations
description: "RAG and AI response quality rules for this project with strict citation requirements. USE FOR: analysis/comparison/chat outputs, prompt design, response contracts, AI provider implementations, and RAG pipeline behavior."
---

# RAG Citations Skill

Use this skill whenever implementing or modifying AI outputs.

## Hard citation requirement

Every AI response for analysis/comparison/chat must include citations with:

1. Document name.
2. Page number.
3. Paragraph reference.
4. Confidence score.

If any field cannot be confidently determined, return a partial citation with explicit uncertainty instead of fabricating details.

## Response quality rules

- Prefer grounded answers over speculative language.
- Link claims to retrieved chunks/snippets.
- Keep citation metadata attached through transformation layers.
- Preserve citation arrays in API contracts and UI view models.

## Implementation guidance

1. Ensure retrieval pipeline returns source metadata needed for citation fields.
2. Ensure provider output mapping preserves source-to-answer traceability.
3. Ensure contracts in Application layer expose citation structure consistently.
4. Ensure UI rendering displays citation fields in a stable, readable format.

## Validation checklist

- Unit tests for citation mapping and serialization.
- Integration tests for end-to-end AI response payload with citations present.
- Negative test for no-result scenarios (empty answer should not include fake citations).
