---
name: cratis-screenplay-toolchain
description: "How to run and check Cratis Screenplay `.play` models: locating the model, choosing the standalone `screenplay` tool or the `cratis` CLI, the exact command behind each verdict V1 to V5 and what a pass looks like, the executable and renderable subsets, reading PLAY, STAGE and CLI-RENDER diagnostics, traps, and a compiled cheat-sheet. Use when choosing a compiler, running or reading a verdict, fixing a diagnostic, or deciding whether syntax binds or renders. Not for: what to model or the phase workflow (use `cratis-screenplay-modeling-lifecycle`), the MCP editing loop (use `cratis-screenplay-model-authoring`), rendering workflow (use `cratis-screenplay-render-and-gap-fill`)."
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-screenplay-toolchain/SKILL.md -->

# Screenplay toolchain

Read [preferred syntax](references/preferred-syntax.md) when writing or reviewing `.play`.

This skill owns tool selection, verdicts and admission. Modeling method lives in
`cratis-screenplay-modeling-lifecycle`; syntax lives in the construct skills.

## Locate the model

Decide opt-in per `general.md` "Model first"; where `cratis-screenplay-modeling-lifecycle` is installed, run its `scripts/model-first-status.mjs` to locate the model root. A folder of `.play`
files is one application: always check the folder, never one file of it.

## Tool versions and the contract

Read the installed versions first: `screenplay --version`, `cratis --version`, and `which -a cratis`
(an older copy earlier on `PATH` shadows a newer install). The standalone tool and the compiler
bundled in the CLI are different releases, and the bundled one can be older; a construct the
standalone tool binds may be refused or unrendered through the CLI. Capabilities that need a
minimum release carry a one-line `Requires ...` note in the reference that teaches them.

Search [screenplay-contract.json](references/screenplay-contract.json), not its
whole contents, for exact diagnostic `code`, tool `name` or `cliCommands` options.
Its version is [screenplay-contract.version](references/screenplay-contract.version).
Read [contract exceptions](references/screenplay-contract-exceptions.json) when a
superseded statement needs context; exceptions do not change the contract.

## Ground rules

1. **Never invent syntax.** Copy shapes from `references/cheat-sheet.md` and the compiled
   examples it names, or from a compiled documentation example. Construct keywords are
   closed.
2. **Check the folder, warnings as errors.** `screenplay <folder> --warnaserror` or
   `cratis screenplay validate <folder> --warnings-as-errors`. Warnings hide defects:
   PLAY0029 (a misspelt keyword or a `type` inside a slice) drops a construct silently.
   A committed `screenplay.json` at the model root declares the repository's gate (checks, warnings
   as errors, scope, open questions): run without repeating those flags (confirm with
   `--print-gate` before dropping `--warnaserror`), read it with
   `--print-gate`, and treat a refused file as "could not run" (`references/verdicts.md`).
3. **Pick the compiler deliberately and name it.** Standalone `screenplay` for
   language checks and reference specifications; `cratis` for rendering
   and generation. Report both tool and version, rather than transferring a verdict across
   releases.
4. **Know the mode** (design, executable, renderable) and write from its subset.
5. **Capability is not correctness.** The tool you ran decides what is accepted; the
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
| Scope/checks | `--scope <address>`, `--check <names>`; a scoped pass is not whole-application success | `validate --executable` (bundled binder, no write), `--check <names>`, `--scope`; `render --scope`/`--check`; `cratis screenplay conform`, `cratis screenplay conform-namespace` |
| Declared gate | `screenplay <root> --print-gate`, `--format json`, `--no-gate-file` | `validate <root> --print-gate`, `--no-gate-file`; the same `screenplay.json` |
| Reference execution | `screenplay test <root>` or MCP `run-specifications` | Discover bundled MCP capability; target tests are not V4 |
| MCP | `screenplay mcp <root>`; discover schemas | `cratis screenplay mcp [root]`; registered by `cratis ai install` |
| Render boundary | Binder admission is not Stage admission | The CLI renders only ESM versions it knows; a newer bound model gets `CLI-RENDER-004` |

Read `references/verdicts.md` for exact commands, exit codes, the MCP roots caveat and the model notes gate.

Requires Cratis CLI 3.57.0 or later (`cratis --version`) to render models that use equality filters or concept generation strategies (ESM v14 and v15); an older CLI answers `CLI-RENDER-004`. Requires Cratis CLI 3.74.0 for ESM v21–v26 and 3.75.0 for v27 (v28 and later: `CLI-RENDER-004`); above the CLI's ceiling, a bundled compiler that binds the construct answers `CLI-RENDER-004`, and one that predates it reports the binder's PLAY0268 (Uuid equality before Screenplay 4.205.0). Either way keep the rule and gap-fill.

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

V2 needs MCP; V3 also supports `cratis screenplay validate --executable <root>`
(labeled as the bundled compiler). Read [verdict commands](references/verdicts.md)
for MCP initialization, source identity and reporting. Without MCP, report
`V2 not run: no MCP in this agent`. Report each verdict separately with tool,
version, source identity and any blocking codes; MCP `modelRevision` is semantic,
not the source identity.

Read [observation comparison](references/observation-comparison.md) when asking whether a running namespace holds what the model declares, or when reading a `conform-namespace` verdict. It differs from model comparison (model versus model) and `conform` (model versus code).

## Subsets (write from the one your mode needs)

- **Design**: every construct family parses; use it to model first. Compiled shapes:
  `references/cheat-sheet.md`. Read [references/cheat-sheet-example.md](references/cheat-sheet-example.md) when you need one complete design-mode document with every construct family.
- **Executable** (binds, V3): `references/executable-subset.md`. Optional keyed lookups, unfiltered
  lists and lists narrowed by one `by` key (observable too) bind; caller-supplied equality
  filters on collections bind as ESM v14. Context `from`, single-result
  filters, opaque `scoped to` and a `by` block on a list query remain PLAY0268; a `performer` query is an opaque read left out of the executable model (PLAY0270; [version gate](references/executable-subset.md#code-attachments-what-binds-what-never-does)). Read
  read-surface [equality filters](../cratis-screenplay-read-surface/references/equality-filters.md).
  Pending handlers beside unconditional productions bind as described commands (v23),
  but reached bodies are Unsupported; attached file/inline handlers refuse PLAY0800
  and require constrained implementation attachments (code gap-fill, not future ESM
  admission).
  Requires Screenplay 4.210.0 (`screenplay --version`); older binders use PLAY0268 for this refusal. Read `arguments.subject`, not the message, for the command.
  Eligible protected reads opt into v25; legacy reads and `concurrency` retain PLAY0271.
  Concept compliance binds as v13 (precise refusals: command-surface `references/compliance.md`);
  generated values and `returns` responses bind (ESM v7, `references/generated-responses-example.md`). Read [references/executable-example.md](references/executable-example.md) when you need a small StateChange and StateView model that binds executable, [references/pdl-example.md](references/pdl-example.md) when writing projection forms that bind and run, and [references/automation-translate-example.md](references/automation-translate-example.md) when writing Automation and Translate slices (ESM v6).
- **Renderable** (V5): `references/renderable-subset.md` has the admission table. The CLI admits through
  its newest known ESM version to construct checks; supported UUID/String
  generation/responses, enum numbers, compliance, equality filters and event reactions render.
  Unsupported generated types still get `STAGE-ESM-028`; result combinations get `-038`.
  Newer versions refuse `CLI-RENDER-004`. Run admission
  before claiming a particular model renders (`cratis render --scope` for an admitted part, `--check`
  for no-write planning).
- **Event sources, streams and routes** bind as ESM v8. Operations and exact numeric source mode remain
  unadmitted. Details: `references/sources-and-streams.md`.
- **Public contracts and routes (v9/v10/v11):** public contracts and direction select v9;
  reaction identity, production overrides and observer filters select v10;
  qualifying retained non-route Double literals select v11.
  Flat composite read-model keys select v27 (Requires Screenplay 4.204.0; older binders
  report PLAY0268); one explicit `key` keeps its earlier version. Read
  `references/production-routes-and-filters.md`, [language admission](references/language-admission.md)
  and read-surface `references/named-keys.md`.
- **Enum numbers (v12):** read command-surface `references/enum-numbers.md` for
  pins, stored numbers and Stage output; `references/diagnostics.md` owns PLAY0667–0671.
- **Concept strategies (v15):** read
  [generation strategies](references/generation-strategies-example.md) for Guarded
  freshness and strict fixtures, and command-surface
  `references/generated-values-and-responses.md` for strategy/refusal depth.
- **v17–v21:** recovery/no-events (v17), query Validation errors (v18), refusal
  branches (v19), generated-concept proofs (v20), pattern/random/rule/checksum (v21).
  Read `references/executable-subset.md` for version prerequisites and
  `references/diagnostics.md` for proof/support failures. Binding is not rendering.
  Tool/renderer authors: read [references/generation-checks.md](references/generation-checks.md)
  when discovering strategy options, checking one concept or value, or consuming rule proofs.
- **v28:** `==`/`!=` between Uuid values in `require` and `produces when` select v28
  (Requires Screenplay 4.205.0; `screenplay --version`; older binders refuse PLAY0268).
  Read [language admission](references/language-admission.md) and command-surface
  `references/uuid-equality.md`.
- **v22–v27:** read [language admission](references/language-admission.md) for atomic
  batches, described handlers, text rules, protected reads, chronological command
  Date/DateTime comparisons (Requires Screenplay 4.199.0; `screenplay --version`;
  older binders refuse PLAY0268) and Stage refusals.
  Read [diagnostic arguments](references/diagnostic-arguments.md) for MCP/JSON subjects:
  code and typed arguments, never message parsing.
- **Caller/compliance metadata:** `$identity` aliases caller built-ins;
  additional identity details are metadata and executable reads still refuse.
  Bare `pii`/`secret` replace legacy spellings; `pii personal` repeats one marker
  (PLAY0653). Purposes/checks/processing records and event `subject` are report-only,
  not legal compliance, retention or runtime subject realization. See
  command-surface `references/context.md` and `references/compliance.md`.

## Edit strategy

Read [the MCP authoring loop](../cratis-screenplay-model-authoring/references/mcp-loop.md)
when editing: the identity owner uses typed proposals for catalog-changing edits;
text edits never bypass a refusal. Without MCP, return identity-affecting work
to its owner. Proposals are connection-local; apply is not crash-atomic, and an
unknown apply outcome requires recovery, not retry.

## Top traps

All 44 traps, with fixes: `references/traps.md`. Read `references/traps.md` when diagnosing an unexpected compile, binding, rendering or MCP result.

Read [references/diagnostics.md](references/diagnostics.md) when a PLAY, STAGE or CLI-RENDER diagnostic needs its meaning and fix. When the Screenplay documentation and the compiler disagree, follow the compiler for what is accepted, keep the intended semantics in the model, and record the gap.

1. Clean V1 is not executable: only MCP (V2) or `render` binds.
2. Prefer inline `produces event` for a command-owned event; plain command productions need explicit `for <identifier>`.
3. PLAY0029 is a warning: validate with warnings as errors.
4. Validate the folder; name the compiler.
5. V1 warns on undeclared projection targets (`PLAY0514`) and removal/capture
   event references (`PLAY0166`). Unmapped declared fields can be reported by
   opt-in `--check field-origins` (`PLAY0534`); unknown coverage is skipped, so
   still walk lineage. See diagnostics for editor versus C#-only checks.
6. Without reaction `runs as` an invoke is caller-less. Declare its trusted role
   (ESM v10), keep the gate, and reference-test; CLI/Stage support is separate.
7. Mark read-model identity with trailing `key`, not `identifier`. Otherwise single-instance
   queries must agree on `by`; only with none and an admitted list query can `*Id`
   inference run. Queryless views need `key` (read-surface `references/named-keys.md`).
   Align projection keys; several parts need an untyped `key` block (v27).
8. Projection `key` routes only on `from`; joins never create; `all` is per source.
9. `null` only for optional read-model properties in specifications.
10. `then` events are exhaustive and ordered; after `when append` they list only what followed.
11. Declare an event once, in its producing slice.
12. Quoted `import "glob"` is your own files; unquoted `import Ctx.Event` is a foreign
    contract and does not bind.
13. Pending described handlers bind but do not execute their bodies; attached handlers and `numbers exact` do not bind. Descriptions render as XML doc comments, not behavior.

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

Read [model notes gate](references/verdicts.md#model-notes-gate) when checking
unresolved questions: the standalone tool has `--open-questions fail`; `cratis screenplay validate`
does through `--open-questions report|fail`. It is separate from compilation and `--warnaserror`.
Requires Cratis CLI 3.60.0 or later (`cratis --version`) for that option; older CLIs lack it.

Read [fast path](references/fast-path.md) for the ordered requirements-to-running-app checklist (validate, `render --check`, render, build, run).

## Route near misses

Read [references/neighbouring-skills.md](references/neighbouring-skills.md) when the work is modeling phases, MCP editing, construct depth, rendering or running systems.
