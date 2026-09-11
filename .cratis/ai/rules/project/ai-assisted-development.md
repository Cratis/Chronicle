---
applyTo: "**/*"
---

## AI-assisted development

This repository uses the managed Cratis AI corpus.

- `.cratis/ai.json` selects `cratis/documentation`, `cratis/engineering/csharp`, `cratis/engineering/react`, `cratis/application/csharp`, `cratis/application/react`.
- `.cratis/ai/rules/project.md` and the files in this directory are project-owned guidance shared by every configured harness.
- `.cratis/ai.manifest.json` records only Cratis-managed files and integrations; project rules and custom skills remain user-owned.
- Run `cratis ai status` before updates and review conflicts before using `--force`.
