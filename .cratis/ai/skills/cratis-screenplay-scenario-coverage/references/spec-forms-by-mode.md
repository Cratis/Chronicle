<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/spec-forms-by-mode.md -->
# Specification forms by mode

## Contents

- Design mode
- Executable mode
- Renderable mode

The subsets themselves (what binds, what renders) are owned by `cratis-screenplay-toolchain`
(its executable and renderable subset references); installed versions by its `SKILL.md`. This file
says what that means for writing specs. A form outside the chosen mode is never deleted from a
design model: keep it and record the gap. The detailed Stage 4.24.x rows below
retain historical admission evidence; tool versions are covered in
`cratis-screenplay-toolchain/SKILL.md`. Run actual target admission instead of assuming old restrictions carry over.
Typed example/override forms: `cratis-screenplay-specifications`
`references/examples-and-routes.md`.

| Form | design | executable | renderable |
|---|---|---|---|
| `when <Cmd>` -> `then <Event>` / `then error` / `then denied` | yes | yes | yes |
| `given <Event>` with `for` | yes | yes (needed for keyed views) | yes |
| `given readmodel` | yes | yes (complete instance with key) | only as the query-only lookup below |
| `when append <Event>` | yes | yes | supported single-hop reaction effects/invocation only; fan-out, deeper cascades and other refused assertions retain STAGE-ESM-011 |
| `then readmodel` / `then query` (keyed `ById`, `arguments`, `result`) | yes | yes | yes |
| `then query` on an unfiltered list or a list narrowed by one `by` key (observable too) | yes | yes: no `arguments` for an unkeyed list, one `result` per row stating the read-model identifier, no `result` = empty list | snapshot and observable lists render |
| Collection query with caller-supplied equality `filter`, including observable | yes | yes, ESM v14: same-named scalar property/type, AND with `by`; optional omission allowed, supplied null refused; PLAY0468 checks arguments; row order does not matter | yes on supported snapshot/observable collections; DateTime/filter parameter order refuse -010; independent Stage executor still refuses filtered assertions (-010) |
| Context `from`, single-result filters, `scoped to` or a list-query `by` block | yes | PLAY0268: query context, collection-only filtering, opaque scope or a multipart list lookup are unadmitted | no |
| `then no readmodel <RM> for "<key>"` | yes | yes | no |
| `when query` -> `then result` / `then no result` | yes | yes | same as `then query`: only the query-only cases below |
| `given clock` / `when clock`, `when trigger`, record-source `given`/`when capture` | yes | yes (standalone and any cratis whose compiler admits v6; an older bundle does not, see toolchain `references/executable-subset.md`) | no |
| Event-source capture fixtures | `given <FromEvent>` / `when append <FromEvent>`, not capture records (PLAY0701) | admitted root capture behavior executes; see `public-event-coverage.md` for prerequisites and limits | separate target admission |
| Command spec listing reaction cascade events | yes (v6 semantics) | false PLAY0285 on an older cratis bundle only; see SKILL.md "Version skew" | no: STAGE-ESM-011; generated CommandScenario does not settle reactions |
| `generated <name> = <value>` under `when`, `then returns` (ESM v7) | yes | yes on a compiler that admits v7 (PLAY0268 on an older bundle) | yes on Stage for supported UUID/String strategies and v15 freshness and scalar/named responses; unsupported types/combinations remain -028/-038 |
| v15 generation strategy fixtures and used-source rejection | yes | strict encoding/size/case/version/variant; Contract for invalid pins, Concurrency for reused source | supported strategies/freshness render; invalid pins refuse -011, unpinned unordered/projected assertions -038; source: command-surface `references/generated-values-and-responses.md` |
| `then events in any order` | yes | yes | yes |
| Event `stream` / `no stream`, typed `for` and stream ids | yes | ESM v8 | The CLI reaches Stage route checks (toolchain `references/renderable-subset.md`); routed read-model/query replay now renders; reserved names/unformattable fixtures remain STAGE-ESM-030 |
| Typed examples and overrides | yes | expanded and type-checked before binding; required fields remain required | depends on expanded contract, not fixture reuse |
| `given operation ... fails`, `then operation`, `then compensated` | yes | unadmitted PLAY0268 | gap-fill |
| `then no events`, `when redelivered` | yes | ESM v17; no new facts, input not re-appended | Stage no-event/redelivery subset; fan-out/cascades/constraints/read expectations/refusal redelivery retain -011 |
| Refusal branches, `$refusal` | yes | ESM v19; first matching branch wins | Stage supported subset; -039 for unfaithful refusal details/trigger groups, -011 for specification limits |
| Query `then error` | yes | ESM v18; one query/one error, no result/denial | requires target v18 admission; a v19-capable renderer does not necessarily admit v18 |
| Composite (JSON) values in specs | yes | yes | no |
| Same-primitive `Date`/`DateTime` comparisons in `require`, `produces when` and command or property validation rules (v26; Requires Screenplay 4.199.0; see [date comparisons](../../cratis-screenplay-command-surface/references/date-comparisons.md)) | spec them | binds and executes; a `DateTime` input without an offset is a Validation rejection | Host/executor run it (Requires Stage 4.135.0); renderer STAGE-ESM-046: gap-fill |
| Date rules using `today`, `$context`, mixed `Date`/`DateTime`, concept or reaction-`where` positions; nested paths | spec them in design | rule does not bind: `recorded` | `recorded` |
| Bodied rules, code policies | spec them | spec reported **unsupported**, never passed | no |
| Pending described command handlers (v23) | specify intent | binds; denial/validation execute, body-reaching specs Unsupported ImplementationBody; all-Unsupported is not green | STAGE-IMPL-002 even Stub mode |
| Attached command handlers | spec them in design | blocked: PLAY0800, no spec runs (V3 blocked); permanent constrained-attachment requirement, gap-fill in code | no |
| `file` constraints | spec them in design | blocked: PLAY0268 at binding, no spec runs (V3 blocked) | no |
| Query `performer`s | spec them in design | opaque read: PLAY0270 information, left out of the executable model; a spec that reads it is left out, every other spec still runs ([version gate](../../cratis-screenplay-toolchain/references/executable-subset.md#code-attachments-what-binds-what-never-does); older compilers report PLAY0268) | no |
| Atomic reaction mode (v22) | declare for new models | failed batch adds no facts or view changes; earlier batches/commands remain; `then error` alone is insufficient | v22 STAGE-ESM-016; mode in admitted v23–v27 STAGE-ESM-043 |
| Protected command reads (v25) | declare opt-in for eligible reads | absence Validation; matching intervening facts Conflict; race vectors are not a new spec construct | Cratis C# rendering in a new enough bundle (STAGE-ESM-044 before, or for unpreservable shapes); semantic Host/executor execute admitted reads, non-Error protected requirements STAGE-ESM-045; v25 pair admitted |

Requires Screenplay 4.210.0 (`screenplay --version`) for attached handlers to report PLAY0800; older binders use PLAY0268. Other PLAY0268 families are unchanged.

Requires Screenplay 4.105.0 or later (`screenplay --version`) for list-query specifications, and 4.143.0 or later for collection equality filters (ESM v14).

Requires Screenplay 4.157.0 for v17, 4.160.0 for v19 and 4.161.0 for query validation/error assertions (`screenplay --version`); older compilers refuse those forms.
Requires Stage 4.90.0 for snapshot/live lists, 4.101.0 for no-event specifications, 4.102.0 for refusal branches and 4.103.0 for rendered redelivery; older renderers refuse those shapes.

## Design mode
Write the full intended behaviour, including scenarios the executable subset cannot run
(filtered, scoped and performer-backed views, the last left out of the executable model rather than refused; clock, trigger and capture scenarios when only a
older cratis bundle is available). Mark each such spec in the coverage matrix as
`gap: <reason>` for the narrower modes so the verification and render phases know what will not execute.

## Executable mode
- Every gated spec has `given caller`; local given/expected events have `for`, and so does every plain `produces <Name>` (inline `produces event` omits it and defaults to the identifier). Foreign event fixtures driving captures may omit `for`; a supplied value is opaque source text, not the capture key.
- Each read model has one key shared by its keyed `=> RM optional` queries; the identifier equals
  the projection's effective key (`cratis-screenplay-slice-design` `references/read-model-design.md`
  decision table). View specs assert through a keyed query.
- Run V3 (binding-ready, per `cratis-screenplay-toolchain`); binding-only codes (PLAY0273/0350/0352/0388/0389) appear only there.
- V4 runs through `screenplay test` or MCP `run-specifications`; record expected,
  discovered/selected and passed/failed/unsupported counts. Unbound/unsupported
  is not passed. Operation specs remain authoring-only recorded contracts; refusal/redelivery
  reference execution is independent of the owning target's admission.

## Renderable mode
- Specs use given events, a command `when`, and then events / readmodel / query / error /
  denied. Only supported single-hop `when append` or located redelivery; no `then no readmodel`. `given readmodel` appears only in the
  query-only lookup below.
- Query-only specs (no command `when`) are admitted by Stage in exactly two cases, and
  `when query` and `then query` are the same case: Screenplay normalises `when query` to the
  `then query` model, so the spelling never decides admission.
  - Denial: `given caller` (no role claims), no given events or read models, one query under a
    policy, one scalar key, no `result`, `then denied`.
  - Seeded lookup: no caller, no given events, exactly one `given readmodel` (a complete
    instance, every property, with its identifier equal to the key) and one unprotected query
    over that read model with the same key and exactly one `result`.
  Any other when-less query (for example over given events, or a list) is not admitted:
  prove the view with a command `when` instead and record the rest as a gap.
- One command per `StateChange` and unconditional `produces`: alternative-path specs over
  `produces when` belong to scope that is not renderable (the example's `RecordDeparture` is
  executable, not renderable).
- Prove projections with `given` events + `when <Command>` + `then query` (or `then readmodel`).
  Removal has no admitted form: record it as a gap for renderable scope.
- A `then error` must name a constraint or rule that is actually rendered. Stage admits
  a value constraint only when each target is a single, required, text-typed property (not
  composite, optional, collection or nontext), and rejects a command that produces more than one
  event affected by the same value constraint (claims or releases) in one batch. The number of
  constraints a command carries is not a limit. The example's `OneBoatPerBerthNight` is
  composite (berth and night, one a date), so it does not render: keep the constraint and its
  protection, and record the gap. Check the renderable subset in `cratis-screenplay-toolchain`
  before promising V5.
- One spec maps to one or more generated test classes, snake-cased from the spec name: the
  command class `when_<spec_name>`, plus `when_<spec_name>_is_projected` per `then readmodel`
  and `when_<spec_name>_is_queried` per `then query`; with several read models or queries the
  suffix gains `_into_<read_model>` or `_through_<query>`. A query-only spec renders only its
  `_is_queried` class. V5 reports evidence for every class of each spec, not just the command class.
