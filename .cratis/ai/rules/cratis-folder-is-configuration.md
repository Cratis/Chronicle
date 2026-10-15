---
applyTo: "**/*"
---
<!-- cratis-ai-managed: rules/cratis-folder-is-configuration.md -->

# `.cratis/` is configuration, never a place to produce things

The `.cratis/` folder is a configuration folder, like `.vscode/` or `.github/`. It holds
project configuration and the shared AI corpus itself, and nothing else.

- **Allowed:** `.cratis/ai.json` and `.cratis/ai.manifest.json`, the installed corpus under
  `.cratis/ai/`, and other tool configuration a Cratis tool reads (settings, profiles,
  connection and environment descriptors).
- **Never produce anything there that can end up in production or that a person or
  product consumes as a deliverable:** Screenplay models and `.play` files, source code,
  generated code, specifications, documentation, migrations, assets, data, build output.
  Those belong in the project's own structure: `Source/` or `src/`, `Documentation/`,
  the product's own folders. A Screenplay model goes where the project's `.play` files
  already are, else under `Source/` or `src/`, else in a `Screenplay/` folder at the
  repository root.
- **Work records are not exempt from a home.** Plans, handovers and scratch notes go in
  `.ai-work/`, never in `.cratis/`; see [local-work-artifacts.md](./local-work-artifacts.md).
- **Never hand-edit installed corpus files under `.cratis/ai/`** in a consuming
  repository; they are managed by the channel that installed them.
- If a request would put a deliverable under `.cratis/`, put it in the project structure
  instead and say so in one line; do not ask permission to follow this rule.
- A repository that already keeps a model under `.cratis/screenplay/` keeps working: read
  it where it is, but do not add new deliverables there, and offer to move it to the
  project structure when the user is changing it anyway.
