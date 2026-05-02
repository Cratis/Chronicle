---
applyTo: "Documentation/**/*.md"
---

# How to Write Documentation

Documentation exists for one audience: **developers who need to use the framework** — not the team that built it. Write from the reader's perspective. They want to know *what this does*, *why they should care*, and *how to use it* — in that order.

Every page should answer: "If I were a developer encountering this concept for the first time, what would I need to understand to use it correctly?"

## General

- All documentation lives in the `Documentation/` folder.
- Site is built using [DocFX](https://dotnet.github.io/docfx/).
- Use [Markdown](https://www.markdownguide.org/) with [GitHub Flavored Markdown](https://github.github.com/gfm/).
- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for diagrams — architecture flows, state transitions, and sequence diagrams are far clearer as visuals than as prose.
- Follow [DocFX Markdown](https://dotnet.github.io/docfx/markdown/) guidelines.

## Structure

- Every folder must have its own `toc.yml` for navigation.
- Every folder must have an `index.md` as a landing page with links to subtopics.
- When linking to a folder in `toc.yml`, link to the folder's `toc.yml`, not to `index.md`.
- Use relative links for internal references.

## Writing Style

The project's voice is **direct, practical, and opinionated**. Write like an experienced colleague explaining something to a capable developer — confident but never condescending.

- **Active voice, present tense.** "Chronicle appends the event" not "The event is appended by Chronicle."
- **Emphasize *why* before *how*.** The reason behind a design choice is more valuable than the steps to implement it. A developer who understands the *why* can handle edge cases the docs don't cover.
- **Don't document the obvious.** If the API is self-explanatory, a code example is enough. Save prose for concepts that are genuinely non-obvious or where the reasoning is important.
- Use headings, lists, and code blocks to organize content — dense paragraphs lose readers.
- Use consistent terminology throughout. If it's called an "event source" in one place, don't call it an "event stream" elsewhere.
- Focus on public APIs and features — not internal implementation.
- Do not document third-party libraries.

## Code Examples

- Prefer `record` types for data structures (events, commands, read models) — this matches the actual codebase.
- When specifying `[EventType]`, never add an explicit name argument — just `[EventType]`.
- Never include verbatim code from the repository — APIs may change. Write purpose-built examples.
- Every code example must be complete and correct — no pseudo-code, no `// ...` elisions that leave the reader guessing.

## File Rules

- Always end generated markdown with a single trailing newline in the file content itself.
- Never use shell commands to modify files after writing them.
- Run `verify-markdown.sh` in the Documentation folder after writing to validate links and formatting.
