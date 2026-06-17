---
agent: agent
description: "Write documentation following the Diátaxis framework."
---

# Write Documentation

Write documentation for a feature, component, or concept. Invoke the **write-documentation** skill and follow `.ai/rules/documentation.md`.

## Confirm first

- **Subject**, **audience**, and the **Diátaxis type** — exactly one:
  - **Tutorial** — guided lesson for newcomers
  - **How-to guide** — recipe for a specific task
  - **Reference** — exhaustive, terse technical description
  - **Explanation** — concepts, trade-offs, architecture (the *why*)
- The source files to document.

## Workflow

Clarify type/audience/scope → propose an outline → write. Active voice, present tense, second person; lead with *why*; complete and correct code examples; Mermaid diagrams for non-trivial concepts; descriptive link text; relative links that resolve. Update `toc.yml` and run the documentation verification before considering it done. The skill carries the per-page detail; don't duplicate it here.
