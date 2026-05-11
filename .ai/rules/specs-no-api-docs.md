---
applyTo: "**/for_*/**/*.*, **/when_*/**/*.*"
paths:
  - "**/for_*/**/*.*"
  - "**/when_*/**/*.*"
---

# Specs Documentation Comment Rule

Specs should describe behavior through naming and assertions, not API-style documentation comments.

## Rules

- Never add XML documentation comments in specs (`/// <summary>`, `/// <param>`, `/// <returns>`, `/// <inheritdoc/>`).
- Never add JSDoc blocks in specs (`/** ... */`) for types, functions, methods, or variables.
- Do not add API-style documentation comments in specs in any language.
- Do not convert regular comments to XML docs or JSDoc.
- Keep comments minimal and behavioral; prefer clear spec naming and structure (`for_*`, `when_*`, `and_*`).
