---
name: cratis-screenplay-toolchain
description: "Verified tool facts for checking Cratis Screenplay `.play` models: the pinned versions of the standalone `screenplay` tool and the bundled `cratis` compiler, the exact command behind each verdict V1 to V5, the executable and renderable subsets, a diagnostics fix guide, traps, documentation contradictions, and a compiled cheat-sheet. Use when choosing a compiler, running or reading a verdict, fixing a PLAY or STAGE diagnostic, or deciding whether syntax binds or renders. Not for: what to model or the phase workflow (use `cratis-screenplay-modeling-lifecycle`), the MCP editing loop (use `cratis-screenplay-model-authoring`), rendering workflow (use `cratis-screenplay-render-and-gap-fill`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/SKILL.md -->

# Screenplay toolchain

Read [preferred syntax](references/preferred-syntax.md) when writing or reviewing `.play`.

Which tool to run, what its result means, and what each tool can and cannot admit. This
skill answers "does it compile, bind, render, and which compiler said so". The method
(what to model, in which order, with which gates) lives in
`cratis-screenplay-modeling-lifecycle`; construct syntax lives in the construct skills
(`cratis-screenplay-command-surface`, `-projections`, `-read-surface`,
`-captures-and-reactions`, `-specifications`, `-ui-composition`).

## Locate the model

Look first in the model root: the folder holding the project's `.play` files (else `Source/` or `src/`, else `Screenplay/`), or the root the project set with `mcpServers.screenplay.root` in `.cratis/ai.json`. A folder of `.play`
files is one application: always check the folder, never one file of it.

## Verified product sources

When choosing or upgrading a tool, or checking version-specific capabilities, read `references/versions.md`; it is the only version table, and other skills point to it. For exact diagnostic codes, MCP names/parameters and standalone CLI options, search the large published [references/screenplay-contract.json](references/screenplay-contract.json), pinned by [references/screenplay-contract.version](references/screenplay-contract.version), rather than loading it in full. Locate a diagnostic by its exact `code` (for example `"code": "PLAY0029"`), a tool by its exact `name` (for example `"name": "read-workspace"`), or a command in `cliCommands`; read the matched object for retirement, parameters or options. [references/screenplay-contract-exceptions.json](references/screenplay-contract-exceptions.json) records reasoned exceptions for historical statements; it does not change the current contract.

The standalone pin is Screenplay 4.149.0.
CLI and bundled package pins: `references/versions.md`. The single version table records
released source evidence and historical probes separately; do not relabel an old
render/browser result as a probe of the current packages.

Historical screens-release evidence: CLI 3.40.7 / Stage 4.51.1 / bundled
Screenplay 4.114.0 opened the canonical corpus with `executableReady true`
and the stdio transcript exposed 37 tools; standalone language 4.122.0 was
recorded in that vector. These retained results are not a rerun on CLI 3.53.1
or the current standalone pin.

## Ground rules

1. **Never invent syntax.** Copy shapes from `references/cheat-sheet.md` and the compiled
   examples it names, or from a compiled documentation example. Construct keywords are
   closed.
2. **Check the folder, warnings as errors.** `screenplay <folder> --warnaserror` or
   `cratis screenplay validate <folder> --warnings-as-errors`. Warnings hide defects:
   PLAY0029 (a misspelt keyword or a `type` inside a slice) drops a construct silently.
3. **Pick the compiler deliberately and name it.** Standalone `screenplay` for
   current language checks and reference specifications; `cratis` for rendering
   and generation. Bundled and standalone versions differ (CLI 3.57.0 bundles 4.147.1;
   the standalone pin is 4.149.0). Report both tool and
   version, rather than transferring a verdict across releases.
4. **Know the mode** (design, executable, renderable) and write from its subset.
5. **Capability is not correctness.** The tool you ran decides what is accepted today; the
   domain and the documented semantics decide what is correct. On conflict keep the correct
   model and record a tool gap; never reshape a model to silence a tool.
6. **Never remove `pii`, `secret`, authorization, list queries, automations or rules
   to reach V3 or a render.** Report the verdict as blocked with the codes.
7. **A clean verdict proves only its own layer.** "Not run" with the reason is a valid
   result. Specifications written or bound are never specifications passing.
8. **Let the tool resolve what it can.** Revisions, identities and renames come from the
   tool, never from hand computation; discover tool, version and root once per session and
   reuse them until the root, the tool or a call changes. Re-resolve only when a value actually
   needs to change: a failed or stale call, a different model root, a changed tool, version or
   configuration, or an explicit override.

## The two compilers

| | Standalone `screenplay` | `cratis screenplay ...` |
| --- | --- | --- |
| Check root | `screenplay <root> --warnaserror --no-color` | `cratis screenplay validate <root> --warnings-as-errors -o json-compact` |
| Scope/checks | `--scope <address>`, `--check <names>`; a scoped pass is not whole-application success | `validate --executable` (bundled binder, no write), `--check <names>`, `--scope`; `render --scope`/`--check`; `cratis screenplay conform` |
| Reference execution | `screenplay test <root>` or MCP `run-specifications` | Discover bundled MCP capability; target tests are not V4 |
| MCP | `screenplay mcp <root>`; discover schemas | `cratis screenplay mcp [root]`; registered by `cratis ai install` |
| Render version boundary | Binder admission is not Stage admission | Pinned CLI (`versions.md`) passes through ESM v15 to Stage construct admission; bound newer models: `CLI-RENDER-004` |

Read `references/versions.md` when you need the exact messages, exit codes or the MCP roots bug.

MCP calls below use the `screenplay` server declared in `.cratis/ai/mcp-servers.json`, for example `screenplay` → `read-workspace`.

## Verdicts

Independent results, not a ladder. Read `references/verdicts.md` for the commands and the report format when obtaining or reporting V1–V5 verdicts.

| Verdict | Meaning | How |
| --- | --- | --- |
| V1 | authorable (compiles, warnings as errors) | the folder command above |
| V2 | executable diagnostics read | MCP `open-workspace`, then `read-workspace view=executable-diagnostics` |
| V3 | binding-ready (binds, no blocking executable diagnostics) | `executableReady` true and no error-severity diagnostic in V2; informational PLAY0269/0270 are reported separately |
| V4 | reference specifications run | `screenplay test` or MCP `run-specifications`; unsupported/unbound is not a pass; rendered Debug tests are V5.tests, never V4 |
| V5 | rendered and target-verified | `cratis render`, then Debug build and tests; report admission, publication, build and tests separately |

V2 and V3 need MCP (V3 alone can also be read from `cratis screenplay validate --executable <root>`, labeled as the bundled compiler): send `initialize`, then `notifications/initialized`, and start the
server with a fixed root when the tool is older than 4.63.2 (the roots bug hit 4.63.1 and the
4.60.1 bundled by cratis before 3.28.2; 4.66.0 and later and cratis 3.28.2 are fine). A subagent without MCP reports "V2 not run: no MCP in this agent".

One line per verdict, tool first, for example `V1 pass (screenplay 4.142.0, 3 files)` and
`V3 blocked: PLAY0268 x3 (context filter, enum compliance)`. Name the source identity (commit plus the digest from
the source-identity helper (`cratis-screenplay-modeling-lifecycle` `references/verdicts-and-modes.md` "Source identity")) the verdict ran on; keep it apart from the MCP
`modelRevision`, which is semantic and, without `.screenplay/identities.json`,
changes with the root folder name.

## Subsets (write from the one your mode needs)

- **Design**: every construct family parses; use it to model first. Compiled shapes:
  `references/cheat-sheet.md`. Read [references/cheat-sheet-example.md](references/cheat-sheet-example.md) when you need one complete design-mode document with every construct family.
- **Executable** (binds, V3): `references/executable-subset.md`. Optional keyed lookups, unfiltered
  lists and lists narrowed by one `by` key (observable too); caller-supplied equality
  filters on collections bind as v14 since 4.143.0. Context `from`, single-result
  filters, opaque `scoped to`/`performer` and composite `by` remain PLAY0268. Read
  read-surface [equality filters](../cratis-screenplay-read-surface/references/equality-filters.md). No handler (PLAY0268, with or without `implementation`/`hint`); `reads` and
  `concurrency` do not bind (PLAY0271); concept compliance binds as v13 on standalone
  4.142.0 (since 4.141.0) and the pinned CLI (precise refusals: command-surface `references/compliance.md`);
  v6 constructs bind on Screenplay 4.61 or
  later (standalone, and cratis 3.28.2 or later); generated values and `returns` responses bind (ESM v7)
  from Screenplay 4.68.0 (also in the current CLI bundle) (`references/generated-responses-example.md`). Read [references/executable-example.md](references/executable-example.md) when you need a small StateChange and StateView model that binds executable, [references/pdl-example.md](references/pdl-example.md) when writing projection forms that bind and run, and [references/automation-translate-example.md](references/automation-translate-example.md) when writing Automation and Translate slices (ESM v6).
- **Renderable** (V5): `references/renderable-subset.md` has the current Stage 4.89.1 table and
  explicitly labeled older probes. CLI 3.57.0 admits through v15; supported UUID/String
  generation/responses, enum numbers, compliance, equality filters and event reactions render.
  Unsupported generated types still get `STAGE-ESM-028`; result combinations get `-038`.
  Newer versions refuse `CLI-RENDER-004`. Run admission
  before claiming a particular model renders (`cratis render --scope` for an admitted part, `--check`
  for no-write planning).
- **Event sources, streams and routes** bind as ESM v8 at 4.125.0.
  The pinned CLI/Stage pair (`references/versions.md`) reaches admission through v15, not blanket
  rendering. Operations and exact numeric source mode remain unadmitted.
  Details: `references/sources-and-streams.md` and `references/versions.md`.
- **Released v9/v10 and published v11:** public contracts and direction select v9;
  reaction identity, production overrides and observer filters select frozen v10;
  qualifying retained non-route Double literals select v11 at 4.135.0.
  Composite view keys remain authoring-only (PLAY0268, #599), while one explicit
  `key` binds. Read `references/production-routes-and-filters.md` and read-surface
  `references/named-keys.md`; version freeze and numeric boundaries: `references/versions.md`.

- **Published v12 (4.139.0):** non-positional enum numbers bind; canonical
  `numbers` parallels `values`. Positional pins preserve earlier executable
  bytes/version. Specifications still use names. CLI 3.57.0's compiler binds
  non-positional numbers, and Stage 4.89.1 admits v12 and emits `Member = N`. Syntax:
  command-surface `references/enum-numbers.md`; errors PLAY0667–0670 and opt-in
  `enum-pinning` hint PLAY0671: `references/diagnostics.md`.

- **Published v15 (4.147.0):** concept strategies and Guarded freshness; strict fixture shapes,
  no silent retry. Read [references/generation-strategies-example.md](references/generation-strategies-example.md)
  and command-surface `references/generated-values-and-responses.md` for supported strategies/refusals.
  Response-name continuations ship separately in 4.148.0, outside ESM; Scene runtime binding is not supplied.

- **Caller/compliance metadata** at 4.127.0: `$identity` aliases caller built-ins;
  additional identity details are metadata and executable reads still refuse.
  Bare `pii`/`secret` replace legacy spellings; `pii personal` repeats one marker
  (PLAY0653). Purposes/checks/processing records and event `subject` are report-only,
  not legal compliance, retention or runtime subject realization. See
  command-surface `references/context.md` and `references/compliance.md`.

## Edit strategy

- **Discover the capabilities available** (tool, version, `tools/list`, model root) once,
  then prefer typed, identity-preserving MCP operations (`propose-ast`, `propose-rename`,
  `propose-repair`, `propose-extract-inline-event`).
- **Bounded text edits** are for edits that leave catalog addresses unchanged (descriptions,
  rule and expression bodies, mappings between existing members): a small `.play` diff
  followed by V1. Without `.screenplay/identities.json`, plain text edits are fine. Never use a
  text edit to get around an MCP refusal.
- **Catalog-changing edits** belong to the identity owner (the session that owns MCP). When
  `.screenplay/identities.json` exists, adding, removing or renaming an addressed element
  (declarations, properties of commands, events, read models, composite types and triggers,
  queries, query arguments, specifications), moving declarations, creating, deleting, renaming
  or moving a mapped `.play` document (`IdentityMappingConflict`) and persisted event contract
  evolution go through MCP, and `id` pins alone
  are not enough: a session without MCP returns the request to the owning session.
- AST JSON costs several times the text it replaces; use it where a typed operation is the
  only identity-safe route.
- Proposals are connection-local (at most 16); `apply` writes `.screenplay/identities.json`
  and is not crash-atomic; never retry `apply` after `ApplyOutcomeUnknown`. Loop details:
  `cratis-screenplay-model-authoring`.

## Top traps

All 44, with fixes: `references/traps.md`. Read `references/traps.md` when diagnosing an unexpected compile, binding, rendering or MCP result.

Read [references/diagnostics.md](references/diagnostics.md) when a PLAY, STAGE or CLI-RENDER diagnostic needs its meaning and fix. Read [references/doc-contradictions.md](references/doc-contradictions.md) when the Screenplay documentation and the compiler disagree and you must decide which to trust.

1. Clean V1 is not executable: only MCP (V2) or `render` binds.
2. Prefer inline `produces event` for a command-owned event; plain command productions need explicit `for <identifier>`.
3. PLAY0029 is a warning: validate with warnings as errors.
4. Validate the folder; name the compiler (cli#244, cli#242).
5. V1 warns on undeclared projection targets (`PLAY0514`) and removal/capture
   event references (`PLAY0166`). Unmapped declared fields can be reported by
   opt-in `--check field-origins` (`PLAY0534`); unknown coverage is skipped, so
   still walk lineage. See diagnostics for editor versus C#-only checks.
6. Without reaction `runs as` an invoke is caller-less. Declare its trusted role
   (ESM v10), keep the gate, and reference-test; CLI/Stage support is separate.
7. Mark read-model identity with trailing `key`, not `identifier`. Otherwise single-instance
   queries must agree on `by`; only with none and an admitted list query can `*Id`
   inference run. Queryless views need `key` (read-surface `references/named-keys.md`).
   Align projection keys. Multipart keys remain PLAY0268 (#599).
8. Projection `key` routes only on `from`; joins never create; `all` is per source.
9. `null` only for optional read-model properties in specifications.
10. `then` events are exhaustive and ordered; after `when append` they list only what followed.
11. Declare an event once, in its producing slice.
12. Quoted `import "glob"` is your own files; unquoted `import Ctx.Event` is a foreign
    contract and does not bind.
13. A handler never binds; `numbers exact` never binds; descriptions render only as XML doc comments, never as behavior.

## Gate

Before a verdict leaves this skill: tool and version named; the folder (not a file) was
checked; warnings were treated as errors; each verdict is a result or "not run" with the
reason; blocked verdicts list codes and keep the protection they blocked on.

## Verify

- [ ] The command ran on the folder and its exit status was read, not inferred from output.
- [ ] `screenplay --version` and `cratis --version` were read; `which -a cratis` shows no
      stale copy ahead on `PATH`.
- [ ] Every PLAY0029 and unresolved-name warning is fixed, not silenced.
- [ ] A V3 or render claim names the compiler that bound it.
- [ ] Nothing was removed to pass a verdict.
- [ ] Any new example fence compiles with the standalone tool; unmarked fences also pass
      `cratis screenplay validate --warnings-as-errors`.

Read [model notes gate](references/versions.md#model-notes-gate) when checking
unresolved questions: standalone 4.144.0 has `--open-questions fail`; Cratis CLI
3.53.1 does not. It is separate from compilation and `--warnaserror`.

## Route near misses

- What to model, phases, gates: `cratis-screenplay-modeling-lifecycle`.
- MCP editing loop, proposals, identities: `cratis-screenplay-model-authoring`.
- Construct depth: `cratis-screenplay-command-surface`, `cratis-screenplay-projections`,
  `cratis-screenplay-read-surface`, `cratis-screenplay-captures-and-reactions`,
  `cratis-screenplay-specifications`, `cratis-screenplay-ui-composition`.
- Rendering and gap-fill: `cratis-screenplay-render-and-gap-fill`,
  `cratis-stage-rendering-and-sandbox`.
- Running systems (Chronicle server operations through the `cratis` CLI, with its authorization boundaries): `cratis-chronicle-cli-operations`.

## Lineage

Read `references/provenance.md` when checking attribution or the sources behind this guidance.
