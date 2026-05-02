---
applyTo: "**/*"
---

# Managing AI Rules and Instructions

`.ai/` is the **single source of truth** for all AI assistant configuration in this repository — rules, agents, prompts, skills, and hooks. Everything is written once in `.ai/` and surfaced to each AI tool through symlinks.

> **Never edit files under `.github/` or `.claude/` directly.** Both folders are composed entirely of symlinks. Any direct edit would be lost the next time the symlink target changes, and would diverge from the canonical source.

## Folder structure

```
.ai/                             ← canonical source of truth (edit here)
├── rules/                       ← instruction/rule markdown files
├── agents/                      ← agent definition files
├── prompts/                     ← reusable prompt templates
├── skills/                      ← multi-step skill workflows
├── hooks/                       ← agent lifecycle hooks
└── workflows/                   ← shared CI workflow files

.github/                         ← GitHub Copilot integration (symlinks only — do NOT edit)
├── copilot-instructions.md      ← symlink → ../.ai/rules/general.md
├── instructions/
│   └── <name>.instructions.md   ← symlinks → ../../.ai/rules/<name>.md
├── agents/                      ← symlink → ../.ai/agents
├── prompts/                     ← symlink → ../.ai/prompts
├── skills/                      ← symlink → ../.ai/skills
└── hooks/                       ← symlink → ../.ai/hooks

.claude/                         ← Claude Code integration (symlinks only — do NOT edit)
├── CLAUDE.md                    ← symlink → ../.ai/rules/general.md
├── rules/
│   └── <name>.md                ← symlinks → ../../.ai/rules/<name>.md
├── agents/                      ← symlink → ../.ai/agents
├── prompts/                     ← symlink → ../.ai/prompts
├── skills/                      ← symlink → ../.ai/skills
└── hooks/                       ← symlink → ../.ai/hooks
```

Note: `agents/`, `prompts/`, `skills/`, and `hooks/` are **folder-level** symlinks — adding, renaming, or removing files inside `.ai/` is immediately visible to both tools. Only `rules/` uses individual per-file symlinks (because GitHub Copilot requires the `.instructions.md` suffix, which requires renaming at the symlink level).

## Rule file format

Every rule file in `.ai/rules/` must start with a YAML frontmatter block containing at minimum an `applyTo` field (for GitHub Copilot). Add a `paths` field when the rule should also be scoped for Claude Code.

```markdown
---
applyTo: "**/*.cs"
paths:
  - "**/*.cs"
---

# Rule Title

Rule content here.
```

Use `applyTo: "**/*"` (and omit `paths`) for rules that apply to all files.

## Adding a new rule

1. **Create the canonical file** in `.ai/rules/<name>.md` with the appropriate frontmatter and content.

2. **Create the Copilot symlink** in `.github/instructions/`:

   ```bash
   cd .github/instructions
   ln -s ../../.ai/rules/<name>.md <name>.instructions.md
   ```

3. **Create the Claude symlink** in `.claude/rules/`:

   ```bash
   cd .claude/rules
   ln -s ../../.ai/rules/<name>.md <name>.md
   ```

4. If the rule applies to all files globally (like `general.md`), update the top-level symlinks:
   - `.github/copilot-instructions.md` → `../.ai/rules/general.md`
   - `.claude/CLAUDE.md` → `../.ai/rules/general.md`

## Updating an existing rule

Edit the canonical file in `.ai/rules/<name>.md`. **Do not touch anything in `.github/` or `.claude/`** — the symlinks automatically reflect the change.

## Updating agents, prompts, skills, or hooks

Add, edit, or remove files directly inside the relevant `.ai/` subfolder (`agents/`, `prompts/`, `skills/`, `hooks/`). The folder-level symlinks in `.github/` and `.claude/` pick up the changes automatically — no further steps needed. **Never create or edit these files inside `.github/` or `.claude/` directly.**

## Renaming a rule

1. Rename the file in `.ai/rules/`.
2. Remove the old symlinks and recreate them pointing to the new filename:

   ```bash
   # In .github/instructions/
   rm <old-name>.instructions.md
   ln -s ../../.ai/rules/<new-name>.md <new-name>.instructions.md

   # In .claude/rules/
   rm <old-name>.md
   ln -s ../../.ai/rules/<new-name>.md <new-name>.md
   ```

3. Update any cross-references within other rule files that link to the renamed file by path.

## Symlink path conventions

Symlink targets use **relative paths** from the symlink's location to the canonical file:

| Symlink location | Target prefix |
|---|---|
| `.github/instructions/` | `../../.ai/rules/` |
| `.claude/rules/` | `../../.ai/rules/` |
| `.github/copilot-instructions.md` | `../.ai/rules/` |
| `.claude/CLAUDE.md` | `../.ai/rules/` |

## Propagation and symlinks

The cross-repository propagation workflow reads `.github/instructions/` and `.github/copilot-instructions.md` via the GitHub API. Because the GitHub API returns symlink blob content verbatim (the raw target path string), a naïve script would push path strings instead of actual rule content to target repositories.

The propagation script in this repository handles this correctly: when it encounters a symlink (Git mode `120000`) in the source tree, it resolves the target path and substitutes the real file's SHA before propagating. This means **symlinks work as expected** — target repositories receive the actual instruction content, not path strings.

Both `.claude/` and `.github/instructions/` therefore use symlinks consistently. There is no need to maintain real file copies anywhere.

## Shared workflows

Workflow files intended to be synced to other repositories live in `.ai/workflows/`. They follow the same symlink pattern — the propagate workflow copies `.ai/workflows/` content to target repositories.
