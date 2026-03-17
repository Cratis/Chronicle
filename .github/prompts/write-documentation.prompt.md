---
agent: agent
description: "Write DocFX documentation following the Diataxis framework."
---

# Write Documentation

I need you to write **DocFX documentation** for a feature, component, or concept using the **Diataxis framework**.

## Inputs

- **Subject** — what to document (e.g. "the Projects feature", "the CommandDialog component", "the ConceptAs type")
- **Document type** — one of:
  - **Tutorial** — step-by-step lesson for newcomers
  - **How-to Guide** — recipe for accomplishing a specific task
  - **Reference** — exhaustive technical description (APIs, attributes, configuration)
  - **Explanation** — discussion of concepts, trade-offs, and architecture
- **Target audience** — e.g. new developer, experienced contributor, framework consumer
- **Existing source files** — paste or reference the code to document

## Instructions

Follow `.github/instructions/documentation.instructions.md` and the `write-documentation` skill exactly.

### Workflow

1. **Clarify** — Confirm the document type, audience, goal, and scope. Ask if anything is ambiguous.
2. **Propose structure** — Present an outline (headings + one-line descriptions). Wait for approval.
3. **Write** — Produce full DocFX-compatible Markdown with correct `toc.yml` entries.

### File structure

For a new topic under an existing section, create a folder with an `index.md` and `toc.yml`. Update the parent `toc.yml` to include the new topic.

### Writing style

- Active voice, present tense, second person
- Lead with the most important information
- Every code example must be complete and correct
- Link to related topics using relative paths
- Use Mermaid diagrams for architecture, sequence flows, and state transitions
- Do not document internal implementation details in user-facing docs

### Validation

- Verify `toc.yml` is valid YAML
- Verify all `href` values point to files that exist
- Verify all Mermaid blocks are valid
