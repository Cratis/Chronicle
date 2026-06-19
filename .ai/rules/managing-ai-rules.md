---
applyTo: ".ai/**,.github/**,.claude/**,.agents/**,AGENTS.md,README.md"
paths:
  - ".ai/**"
  - ".github/**"
  - ".claude/**"
  - ".agents/**"
  - "AGENTS.md"
  - "README.md"
---

# Managing AI Rules and Instructions

`.ai/` is the **single source of truth** for all AI assistant configuration in this repository — rules, agents, prompts, skills, and hooks. Everything is written once in `.ai/` and surfaced to each AI tool through adapters: folder symlinks, per-file symlinks, or small **path-reference files** whose body is the relative path to the canonical source.

> **Never edit files under `.github/`, `.claude/`, `.agents/`, or the root `AGENTS.md` directly.** They are all adapters. Any direct edit would be lost the next time the canonical source changes, and would diverge from it.

## Folder structure

```
.ai/                             ← canonical source of truth (edit here)
├── rules/                       ← instruction/rule markdown files
├── agents/                      ← agent definition files
├── prompts/                     ← reusable prompt templates
├── skills/                      ← multi-step skill workflows
├── hooks/                       ← agent lifecycle hooks
└── workflows/                   ← shared CI workflow files

.github/                         ← GitHub Copilot adapters (do NOT edit)
├── copilot-instructions.md      ← path-reference → ../.ai/rules/general.md
├── instructions/
│   └── <name>.instructions.md   ← per-file adapter → ../../.ai/rules/<name>.md   (Copilot needs the .instructions.md suffix)
├── agents/
│   └── <name>.agent.md          ← per-file symlink → ../../.ai/agents/<name>.md   (Copilot needs the .agent.md suffix)
├── prompts/                     ← folder symlink → ../.ai/prompts                (Copilot reads *.prompt.md)
└── skills/                      ← folder symlink → ../.ai/skills

.claude/                         ← Claude Code adapters (do NOT edit)
├── CLAUDE.md                    ← symlink → ../.ai/rules/general.md
├── rules/
│   └── <name>.md                ← per-file symlink → ../../.ai/rules/<name>.md
├── agents/                      ← folder symlink → ../.ai/agents                 (Claude reads <name>.md)
├── commands/
│   └── <name>.md                ← per-file symlink → ../../.ai/prompts/<name>.prompt.md   (Claude slash commands)
└── skills/                      ← folder symlink → ../.ai/skills
                                    (hooks: Claude wires them in .claude/settings.json — no folder adapter)

.agents/                         ← Codex adapters (do NOT edit)
└── skills/                      ← folder symlink → ../.ai/skills

AGENTS.md                        ← Codex root instructions → .ai/rules/general.md
```

**Each tool has its own conventions, so adapters differ by surface** (verified against each tool's docs):

| Surface | Copilot | Claude Code | Codex |
|---|---|---|---|
| Root instructions | `copilot-instructions.md` → `general.md` | `CLAUDE.md` → `general.md` | `AGENTS.md` → `general.md` |
| Scoped rules | `instructions/<n>.instructions.md` (per-file) | `rules/<n>.md` (per-file) | — |
| Agents | `agents/<n>.agent.md` (per-file, `.agent.md` suffix) | `agents/` (folder symlink, `<n>.md`) | — |
| Prompts / commands | `prompts/` (folder symlink, `*.prompt.md`) | `commands/<n>.md` (per-file) | — |
| Skills | `skills/` (folder symlink) | `skills/` (folder symlink) | `.agents/skills/` (folder symlink) |
| Hooks | `.github/hooks/*.json` | `.claude/settings.json` | — |

Folder symlinks (skills both sides, Copilot prompts, Claude agents) pick up additions/renames automatically. The per-file adapters (rules, Copilot agents, Claude commands) are needed because the tool requires a different filename suffix/location than the canonical source — so a new rule/agent/prompt needs its matching per-file adapter created (see below). The validator (`hooks/scripts/validate-ai-setup.sh`) checks each adapter resolves to the right canonical file (symlink or path-reference file are both accepted).

> **Hooks are not folder adapters.** Markdown is not a hook format for either tool — `.ai/hooks/*.md` are *lifecycle guidance*. Enforce them per tool: Claude via `.claude/settings.json` (`Stop`, `PreToolUse`, …); Copilot via `.github/hooks/*.json` (`sessionStart`/`sessionEnd`/`userPromptSubmitted`).

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

### Profiles (application vs framework)

A Cratis repo is one of two **profiles** and the corpus serves both from this one source:

- **application** — building an app *on* Cratis (event-sourced CQRS, vertical slices, MVVM frontend). The bulk of the rules.
- **framework** — contributing to a Cratis framework repo *itself* (Arc, Chronicle, Fundamentals, Components — libraries). See `framework.md`.

A profile-specific rule declares **`profile: application`** or **`profile: framework`** in its frontmatter; a rule with **no `profile:` is universal** and applies in both. `general.md` routes by profile (its application sections are clearly bannered; `framework.md` is the framework counterpart). `applyTo`/`paths` globs scope by *file type*; `profile:` scopes by *repo type* — both are needed because every repo has `.cs`/`.tsx` files. (Propagation can later filter by profile so a framework repo receives only `universal` + `framework`; until then, the `general.md` routing + per-rule banners make the AI self-select.)

## Adding a new rule

1. **Create the canonical file** in `.ai/rules/<name>.md` with the appropriate frontmatter and content.

2. **Create the Copilot adapter** in `.github/instructions/` — a path-reference file whose body is the relative target:

   ```bash
   printf '%s' "../../.ai/rules/<name>.md" > .github/instructions/<name>.instructions.md
   ```

   (A symlink — `ln -s ../../.ai/rules/<name>.md <name>.instructions.md` — also resolves; the repo standardizes on path-reference files.)

3. **Create the Claude symlink** in `.claude/rules/`:

   ```bash
   cd .claude/rules
   ln -s ../../.ai/rules/<name>.md <name>.md
   ```

4. If the rule applies to all files globally (like `general.md`), update the top-level adapters:
   - `.github/copilot-instructions.md` → `../.ai/rules/general.md`
   - `.claude/CLAUDE.md` → `../.ai/rules/general.md`

5. **Codex needs no per-rule step** — it consumes only `AGENTS.md` (→ `general.md`) and `.agents/skills` (→ `.ai/skills`), both already wired. New skills are picked up automatically through the `.agents/skills` folder symlink.

## Updating an existing rule

Edit the canonical file in `.ai/rules/<name>.md`. **Do not touch anything in `.github/` or `.claude/`** — the symlinks automatically reflect the change.

## Updating agents, prompts, skills, or hooks

Always edit the canonical file in the relevant `.ai/` subfolder — never the adapters. Whether you need a new adapter depends on the surface:

- **Skills** (`.ai/skills/<n>/SKILL.md`) — folder symlinks on all three sides pick up new/renamed skills automatically. No adapter step.
- **Agents** (`.ai/agents/<n>.md`) — Claude's `.claude/agents` folder symlink is automatic, but **Copilot needs a per-file `.agent.md` adapter**:
  ```bash
  ln -s ../../.ai/agents/<n>.md .github/agents/<n>.agent.md
  ```
- **Prompts** (`.ai/prompts/<n>.prompt.md`) — Copilot's `.github/prompts` folder symlink is automatic, but **Claude needs a per-file command adapter**:
  ```bash
  ln -s ../../.ai/prompts/<n>.prompt.md .claude/commands/<n>.md
  ```
- **Hooks** (`.ai/hooks/*.md`) — these are *guidance*, not wired hooks. To enforce, add the real artifact per tool (Claude `.claude/settings.json`; Copilot `.github/hooks/*.json`).

Run `hooks/scripts/validate-ai-setup.sh` after adding an agent or prompt to confirm its adapter resolves.

## Renaming a rule

1. Rename the file in `.ai/rules/`.
2. Remove the old symlinks and recreate them pointing to the new filename:

   ```bash
   # In .github/instructions/ (path-reference file)
   rm <old-name>.instructions.md
   printf '%s' "../../.ai/rules/<new-name>.md" > <new-name>.instructions.md

   # In .claude/rules/ (symlink)
   rm <old-name>.md
   ln -s ../../.ai/rules/<new-name>.md <new-name>.md
   ```

3. Update any cross-references within other rule files that link to the renamed file by path.

## Adapter path conventions

An adapter's target (the symlink target, or the path-reference file's body) uses a **relative path** from the adapter's location to the canonical file:

| Adapter location | Target |
|---|---|
| `.github/instructions/<name>.instructions.md` | `../../.ai/rules/<name>.md` |
| `.claude/rules/<name>.md` | `../../.ai/rules/<name>.md` |
| `.github/agents/<name>.agent.md` | `../../.ai/agents/<name>.md` |
| `.claude/commands/<name>.md` | `../../.ai/prompts/<name>.prompt.md` |
| `.github/copilot-instructions.md` | `../.ai/rules/general.md` |
| `.claude/CLAUDE.md` | `../.ai/rules/general.md` |
| `AGENTS.md` (repo root, Codex) | `.ai/rules/general.md` |
| `.agents/skills`, `.github/prompts`, `.github/skills`, `.claude/agents`, `.claude/skills` (folder symlinks) | the matching `.ai/<sub>` folder |

## Propagation and adapters

The cross-repository propagation workflow is a broadcast sync: any Cratis repository can be the source, and changes propagate to the other repositories, including `Cratis/AI` when the source is not `Cratis/AI`. Propagation normalizes known adapter paths before broadcasting them. When the matching canonical `.ai` file exists in the source tree, an adapter path is written as the expected symlink or path-reference file, even if the source repository currently contains copied content at that adapter path. Do not materialize adapter targets into copied `.ai` content in tool-specific files; that breaks the `.ai/` source-of-truth model and causes drift between adapters and canonical corpus files.

## Shared workflows

Workflow files intended to be synced to other repositories live in `.ai/workflows/`. They follow the same symlink pattern — the propagate workflow copies `.ai/workflows/` content to target repositories.
