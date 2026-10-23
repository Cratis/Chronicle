<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/parsed-not-runnable.md -->
# Parsed is not runnable: ESM versions and dispositions

Checked: `SemanticModelBinder.cs`, `Versions.cs`,
`Documentation/screenplay/{commands,operations,event-sources,reactions,tool,interoperability}.md`.
The binder selects v8 for event routes, v10 for declared reaction identity, v11 for retained
exact-lowered Double-mode literals and v12 for non-positional enum numbers.
Compliance (`SemanticModelBinder.Compliance.cs` and
`SemanticModelValidator.Compliance.cs`) selects v13.
Tool versions and consumer boundaries: toolchain `SKILL.md`.

## Which ESM version a model needs

- v1 by default; v2 for typed event-source facts and admitted occurrence/audit mappings.
- v3 for opaque code requirements. Reference execution cannot execute their bodies.
- v4 for event-contract lineage; v5 for keyed read-model absence assertions.
- v6 for admitted reactions, captures, application triggers and clock.
- v7 for generated command values, responses, fixtures/return assertions and policy negation.
- v8 for sources/streams and command/specification routes.
- v9 for public-event contracts; v10 for reaction `runs as system` identity.
- v11 for exact lowering of qualifying Double-mode number literals; v12 for non-positional
  enum member numbers (`cratis-screenplay-command-surface` `references/enum-numbers.md`).
- v13 for concept `pii`/`personal` and `secret`, personal qualifiers and secret scopes.
  Reasons remain report-only PLAY0270; binding proves neither encryption nor erasure.
- v17 for `when redelivered` and `then no events`; v19 for reaction
  refusal branches and `$refusal` values. Older compilers report PLAY0268.
- Operations/systems and the exact numeric **source mode** (`numbers exact`) remain unadmitted
  (PLAY0268). Version allocation is not inferred from an older planning decision.

## Dispositions to keep apart

| Construct | Standalone disposition |
| --- | --- |
| `persona`, domain/authentication metadata | Report-only; unknown named policies remain source defects |
| Compliance concepts | Bare `pii`/`personal` and `secret`, qualifiers and scope bind as v13; enum/identity compliance and events combining personal or explicitly subject-scoped secret values with an explicit `subject` mark refuse PLAY0268 at binding only. A CLI bundle that admits v13 binds markers; Stage renders v13 protection, not protected identities/enums or subject lineage. Never remove protection |
| Identity details / processing purposes / event subject | Report-only metadata; executable detail reads refuse PLAY0268, not source declaration errors |
| Composite stream ids | v8 routing; complete named parts/canonical comparisons, PLAY0515 refuses protected keys/sources; not a legacy text split |
| Command reads/concurrency | Eligible reads opt into v25 with `command reads protected`; legacy reads/concurrency retain PLAY0271; the Cratis renderer renders protected reads as Arc read protection in a new enough bundle (older bundles, and shapes it cannot preserve, refuse STAGE-ESM-044); semantic Host/executor support is separate |
| Invokes-only trigger reads | Report-only, not protected state; direct production with reads refuses |
| Generated values/responses | v7 binding/reference execution; v15 strategies/strict fixtures/freshness. Stage renders supported UUID/String strategies, responses and fixtures; unsupported types/combinations remain -028/-038 |
| Sources/streams and command/specification routes | v8 binding/reference execution; the CLI (toolchain `references/renderable-subset.md`) reaches Stage checks through its bundled ceiling (v16 without v17 support) |
| Declared reaction invocation actor | v10 binding/reference execution; older CLI parser PLAY0137; Stage renders role/authenticated reaction identity; claim-reading/opaque gates retain STAGE-ESM-034, not a v10 version refusal |
| Redelivery and `then no events` | v17 binding/reference execution; older compilers refuse PLAY0268 |
| Reaction refusal branches and `$refusal` | v19 binding/reference execution; older compilers refuse PLAY0268 |
| Operations/systems, `numbers exact` source mode | Source accepted; whole binding refuses PLAY0268 |
| UI | Authoring/deferred metadata; runtime support is a separate consumer contract |

Run standalone `screenplay test` or MCP `run-specifications` for V4. Compile
success alone is not execution; unbound/unsupported is not passed. Preserve
source that a narrower renderer cannot realize and report the addressed gap.
Routing and actor checked examples: toolchain `references/sources-and-streams.md`
and captures-and-reactions `references/invocation-identity.md`.

## The Step 7 clock example

The complete example in [nine-steps.md](nine-steps.md) (Step 7) compiles and binds with zero errors/warnings and `executableReady true`.
Its invokes-only reads still record intent rather than execute the overdue
selection. Run its admitted specifications with the current reference route
before claiming a V4 pass; no overdue-selection proof follows from binding.

## Trigger reads and event generations

Trigger reads do not enforce decision consistency. Event generations and typed repairs now ship,
but historical schemas do not supply deployed migrations.
Check actual binder, runner and consumer admission instead of equating an
accepted proposal or parsed node with a running application.


## What binds depends on which tool

- `Automation` and `Translate` slices, reactions, captures and triggers bind (ESM v6) on the
  standalone tool and a CLI bundle that admits v6. (An older bundle
  reports *Slice '<name>' of type '<type>' is not admitted by ESM v1.*) Stage renders supported event-triggered Automation, not Translate/captures or non-event triggers.
- Eligible command reads bind/reference-execute with `command reads protected`
  (v25); legacy reads and `concurrency` remain PLAY0271. Read command-surface
  [protected reads](../../cratis-screenplay-command-surface/references/protected-reads.md)
  for admission, absence/conflicts and Stage refusal. A reaction trigger's `reads` that only `invokes` is
  report-only intent (`PLAY0270`); one that `produces` directly fails binding (`PLAY0268`).
- `persona` declarations are report-only and never block; bare `pii` and `secret`
  concepts bind as ESM v13 (Requires Screenplay 4.141.0 or later); Stage renders admitted v13 protection.
  Enum/identity compliance and protected events with explicit subject marks remain
  refused (PLAY0268); see command-surface `references/compliance.md`. Keep protection.
- Generated values/responses bind and reference-execute as v7; sources, streams
  and command/specification routes as v8; reaction `runs as` identity as v10;
  qualifying Double literals lower exactly as v11 and non-positional enum numbers
  as v12. Use `screenplay test` or MCP `run-specifications` for V4.
  Operations/systems and the `numbers exact` **source mode** remain unadmitted
  (PLAY0268); redelivery/no-event assertions execute as v17 and refusals as v19. The CLI and Stage render supported generation strategies and v15 freshness,
  scalar/named responses, enum numbers, compliance and supported event reactions.
  The pair reaches construct checks through the ESM version it bundles, not blanket rendering; supported v14 filters
  render. Current boundaries and source evidence:
  `cratis-screenplay-toolchain/references/renderable-subset.md`.
  Source/binding admission and target realization are separate gates.

## A CLI and Stage that admit v15

      A CLI and Stage that admit v15 also admit supported event Automation,
      generation strategies/responses, equality filters, enum numbers and compliance. Translate/captures
      and refused reactions remain gaps (Stage `references/admission.md`).
      Mixed effects remain `STAGE-ESM-037`: retries can duplicate events. Keep the reaction
whole and prove retry-safe gap-fill; never split or drop effects.
