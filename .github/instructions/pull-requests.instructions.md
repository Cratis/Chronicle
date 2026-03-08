---
applyTo: "**/*"
---

# How to Do Pull Requests

PR descriptions serve two purposes: they help reviewers understand the change *now*, and they become the release notes that users read *later*. Write them with both audiences in mind.

## Description

- Follow the [pull request template](../pull_request_template.md).
- Focus on the **Added**, **Changed**, **Fixed**, **Removed**, **Security**, and **Deprecated** sections. Remove sections that are empty — don't leave blank headings.
- Each bullet should be a complete, self-contained statement that makes sense without context. A reader scanning release notes should understand what changed from the bullet alone.
- Add a link to the issue on every bullet point (e.g. `(#123)`) at the end, if applicable.
- Include a summary only if there is a cohesive theme across the changes. If you find yourself restating individual bullets in slightly different words, the summary adds no value — remove it.

## Commits

- Write clear, concise commit messages in imperative mood: "Add author registration" not "Added author registration" or "Adding author registration".
- Each commit should represent a logical unit of work. Avoid "WIP" or "fix" commits in the final PR — squash or rebase if needed.

## Labels

- Label the PR according to semantic versioning impact:
  - **major** — breaking changes to public APIs
  - **minor** — new features, new slices, non-breaking additions
  - **patch** — bug fixes, docs, refactoring with identical behavior

## Quality Gates

Before marking a PR ready for review:
- `dotnet build` — zero errors, zero warnings
- `dotnet test` — all specs pass
- `yarn lint` — zero errors
- `npx tsc -b` — zero TypeScript errors
- Code follows all project coding standards and conventions
