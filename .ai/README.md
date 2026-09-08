# Shared AI Assistant Configuration

`.ai/` is the **single source of truth** for all AI-assistant configuration — rules, agents, prompts, skills, hooks. Everything is written once here and surfaced to each tool through adapters (path-reference files or symlinks). Edit the canonical source when a tool file or root `AGENTS.md` is a symlink/path-reference adapter. Preserve and deliberately maintain regular repository-owned bootstraps and private overlays; never patch generated immutable distribution output.

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

Legacy Copilot/Claude/Codex adapters use **symlinks** or **path-reference files** to their canonical `.ai/` sources. Pi agent adapters are instead generated real files from this checkout’s own `.ai/agents`; their generated bodies are not independent authoring sources.

Each tool has its own conventions, so adapters differ by surface (see `rules/managing-ai-rules.md` for the full table):

- **GitHub Copilot** — `copilot-instructions.md` + `instructions/<n>.instructions.md` (rules); `agents/<n>.agent.md` (per-file, `.agent.md` suffix); `prompts/` + `skills/` (folder symlinks); hooks as `.github/hooks/*.json`.
- **Claude Code** — `CLAUDE.md` + `rules/<n>.md` (rules); `commands/<n>.md` (slash commands, from `.ai/prompts`); `agents/` + `skills/` (folder symlinks); hooks in `.claude/settings.json`.
- **Codex** — root `AGENTS.md` → `.ai/rules/general.md`; `.agents/skills` → `.ai/skills`.
- **Pi agents** — generated real `.pi/agents/*.md` files; never edit them or their manifest directly. Use the reviewed `Cratis/AI` generator from an explicitly available checkout, with an absolute `--repo` for this repository and no automatic download/broadcast. See [the generator procedure](rules/managing-ai-rules.md#pi-generated-local-agent-adapters). Every generated adapter sets `extensions: false` and `skills: false`; planners/coordinators return plans to the parent rather than executing or delegating them.

`.ai/hooks/*.md` are **lifecycle guidance**, not wired hooks (markdown isn't a hook format for either tool); enforce them via each tool's real hook mechanism above.

## Scoped rule frontmatter

Scoped rules include both `applyTo` (Copilot matching) and `paths` (Claude matching). Use `applyTo: "**/*"` (and omit `paths`) for all-files rules. `general.md` is the frontmatter-less root.

## Validation

Run `.ai/hooks/scripts/validate-ai-setup.sh` after changing rules/skills/adapters — it validates frontmatter, adapter integrity (path-reference *or* symlink resolving to the right rule), resolving adapter targets, Codex adapters, and content-drift guards (warnings). Structural/adapter/Codex failures are fatal; drift guards are advisory warnings. Fix reported issues before committing.

## Distribution and local adapters

Cross-repository broadcast, all-to-all propagation, and reverse synchronization
are retired. Do not run legacy propagation or turn a consuming repository into a
hub. Shared public-safe behavior is authored and reviewed in `Cratis/AI`, generated
into `Cratis/AI.Distribution`, and consumed only at an immutable reviewed version
after release gates pass. Propose sanitized reusable improvements upstream for
review; never reverse-sync private trees or local facts.

These legacy repository-local rules remain locally maintained during canary;
this is not permission to patch generated immutable distribution bytes or copy
whole AI trees. Preserve private/project overlays, local skills, and minimal
host bootstraps. Keep legacy adapters and actual workflows in place until an
approved replacement passes canary and reviewed retirement gates. Update shared
packages via approved exact-version pins; roll back by version.

See `rules/managing-ai-rules.md` for the full guide on adding, updating, and renaming rules/skills/agents/prompts/hooks.
