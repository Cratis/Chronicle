# Shared AI Assistant Configuration

`.ai/` is the **single source of truth** for all AI-assistant configuration — rules, agents, prompts, skills, hooks. Everything is written once here and surfaced to each tool through adapters (path-reference files or symlinks). **Never edit files under `.github/`, `.claude/`, `.agents/`, or root `AGENTS.md` directly** — they are adapters, and any direct edit is lost the next time the source changes.

## Authority model

A layered hierarchy:

1. `rules/general.md` — project-wide non-negotiables and the implementation gates (the always-on root).
2. `rules/*.md` — scoped invariants (C#, slices, React, specs, docs, …).
3. `skills/*/SKILL.md` — task workflows, sequencing, examples, checklists.
4. `agents/`, `prompts/`, `hooks/` — **entrypoints that point back to canonical rules and skills, not redefine them.**

A skill may refine *how* to apply a rule, but must not contradict a non-negotiable rule. **If a skill and a rule conflict, treat it as drift: follow the stricter invariant and fix the stale artifact.**

## Three levels of authority (content)

Every rule is one of: **Framework contract** (enforced by Arc/Chronicle source/analyzers/runtime) · **Cratis convention** (house default for maintainability — the framework does not enforce it) · **Product policy** (belongs in a downstream app's own `.ai/`, not here). Rules state which they are; never claim "the framework requires" a convention.

## Profiles

The corpus serves two repo types from one source: **application** (building *on* Cratis — event-sourced vertical slices) and **framework** (contributing to Cratis libraries — Arc/Chronicle/Fundamentals/Components, see `rules/framework.md`). A rule declares `profile: application` or `profile: framework`; rules with no `profile:` are universal. `general.md` routes by profile; `applyTo`/`paths` scope by file type, `profile:` by repo type.

## Structure

- `rules/` — instruction files · `prompts/` — reusable prompts · `agents/` — agent definitions · `skills/` — multi-step workflows · `hooks/` — lifecycle hooks · `hooks/scripts/` — validation.

## Tool integration (adapters)

Each adapter resolves to its canonical `.ai/` file. It may be a **symlink** or a **path-reference file** (a small file whose body is the relative target path) — both forms are accepted; what matters is that it resolves to the right canonical file.

Each tool has its own conventions, so adapters differ by surface (see `rules/managing-ai-rules.md` for the full table):

- **GitHub Copilot** — `copilot-instructions.md` + `instructions/<n>.instructions.md` (rules); `agents/<n>.agent.md` (per-file, `.agent.md` suffix); `prompts/` + `skills/` (folder symlinks); hooks as `.github/hooks/*.json`.
- **Claude Code** — `CLAUDE.md` + `rules/<n>.md` (rules); `commands/<n>.md` (slash commands, from `.ai/prompts`); `agents/` + `skills/` (folder symlinks); hooks in `.claude/settings.json`.
- **Codex** — root `AGENTS.md` → `.ai/rules/general.md`; `.agents/skills` → `.ai/skills`.

`.ai/hooks/*.md` are **lifecycle guidance**, not wired hooks (markdown isn't a hook format for either tool); enforce them via each tool's real hook mechanism above.

## Scoped rule frontmatter

Scoped rules include both `applyTo` (Copilot matching) and `paths` (Claude matching). Use `applyTo: "**/*"` (and omit `paths`) for all-files rules. `general.md` is the frontmatter-less root.

## Validation

Run `.ai/hooks/scripts/validate-ai-setup.sh` after changing rules/skills/adapters — it validates frontmatter, adapter integrity (path-reference *or* symlink resolving to the right rule), resolving adapter targets, Codex adapters, and content-drift guards (warnings). Structural/adapter/Codex failures are fatal; drift guards are advisory warnings. Fix reported issues before committing.

## Propagation

This repo is the hub that can propagate `.ai/` content to other Cratis repositories. The propagation workflow and any profile/repo-type scoping are managed separately from the corpus content itself — see `rules/managing-ai-rules.md` for how propagation interacts with adapters.

See `rules/managing-ai-rules.md` for the full guide on adding, updating, and renaming rules/skills/agents/prompts/hooks.
