<!-- cratis-ai-managed: skills/cratis-screenplay-scenario-coverage/references/spec-forms-by-mode.md -->
# Specification forms by mode

The subsets themselves (what binds, what renders) are owned by `cratis-screenplay-toolchain`
(its executable and renderable subset references); version facts by its `references/versions.md`. This file
says what that means for writing specs. A form outside the chosen mode is never deleted from a
design model: keep it and record the gap. The detailed Stage 4.24.x rows below
retain historical admission evidence; current pins are in
`cratis-screenplay-toolchain/references/versions.md`. Run actual target admission instead of assuming old restrictions carry over.
Typed example/override forms: `cratis-screenplay-specifications`
`references/examples-and-routes.md`.

| Form | design | executable | renderable |
|---|---|---|---|
| `when <Cmd>` -> `then <Event>` / `then error` / `then denied` | yes | yes | yes |
| `given <Event>` with `for` | yes | yes (needed for keyed views) | yes |
| `given readmodel` | yes | yes (complete instance with key) | only as the query-only lookup below |
| `when append <Event>` | yes | yes | supported single-hop reaction effects/invocation only; fan-out, deeper cascades and other refused assertions retain STAGE-ESM-011 |
| `then readmodel` / `then query` (keyed `ById`, `arguments`, `result`) | yes | yes | yes |
| `then query` on an unfiltered list or a list narrowed by one `by` key (observable too) | yes | yes since 4.105.0: no `arguments` for an unkeyed list, one `result` per row stating the read-model identifier, no `result` = empty list | observable lists render, a non-observable list is `STAGE-ESM-010` (Stage 4.73.1) |
| Collection query with caller-supplied equality `filter`, including observable | yes | yes since 4.143.0, ESM v14: same-named scalar property/type, AND with `by`; optional omission allowed, supplied null refused; PLAY0468 checks arguments; row order does not matter | yes on supported observable collections in CLI 3.57.0 / Stage 4.89.1; DateTime/filter parameter order and snapshot lists refuse -010; independent Stage executor still refuses filtered assertions (-010) |
| Context `from`, single-result filters, `scoped to`, `performer` or composite `by` | yes | PLAY0268: query context, collection-only filtering, opaque scope/implementation or multipart lookup are unadmitted | no |
| `then no readmodel <RM> for "<key>"` | yes | yes | no |
| `when query` -> `then result` / `then no result` | yes | yes | same as `then query`: only the query-only cases below |
| `given clock` / `when clock`, `when trigger`, `given`/`when capture` | yes | yes (standalone and any current cratis; history: a cratis before 3.28.2 bundle no, see versions.md) | no |
| Command spec listing reaction cascade events | yes (v6 semantics) | false PLAY0285 on a cratis before 3.28.2 bundle only; see SKILL.md "Version skew" | no: STAGE-ESM-011; generated CommandScenario does not settle reactions (Stage 4.89.1) |
| `generated <name> = <value>` under `when`, `then returns` (ESM v7) | yes | yes on standalone Screenplay 4.68.0 or later and the current cratis bundle (history: PLAY0268 on the 4.66.0 bundled in cratis 3.28.x) | yes on Stage 4.89.1 for supported UUID/String strategies and v15 freshness and scalar/named responses; unsupported types/combinations remain -028/-038 |
| v15 generation strategy fixtures and used-source rejection | yes | strict encoding/size/case/version/variant; Contract for invalid pins, Concurrency for reused source | supported strategies/freshness render; invalid pins refuse -011, unpinned unordered/projected assertions -038; source: command-surface `references/generated-values-and-responses.md` |
| `then events in any order` | yes | yes | yes |
| Event `stream` / `no stream`, typed `for` and stream ids | yes | ESM v8 at 4.125.0 | The pinned CLI reaches Stage route checks (toolchain `references/versions.md`); routed read-model/query replay now renders (Stage 4.89.1); reserved names/unformattable fixtures remain STAGE-ESM-030 |
| Typed examples and overrides | yes | expanded and type-checked before binding; required fields remain required | depends on expanded contract, not fixture reuse |
| `given operation ... fails`, `then operation`, `then compensated` | yes | unadmitted PLAY0268 | gap-fill |
| Refusal branches, `when redelivered`, `then no events` | yes | unadmitted PLAY0268 | gap-fill |
| Composite (JSON) values in specs | yes | yes | no |
| Rules over dates or `today`, nested paths | spec them in design | rule does not bind: `recorded` | `recorded` |
| Bodied rules, code policies | spec them | spec reported **unsupported**, never passed | no |
| Command handlers, `file` constraints, query `performer`s | spec them in design | blocked: PLAY0268 at binding, no spec runs (V3 blocked) | no |

## Design mode
Write the full intended behaviour, including scenarios the executable subset cannot run
(filtered, scoped and performer-backed views; clock, trigger and capture scenarios when only a
cratis before 3.28.2 bundle is available). Mark each such spec in the coverage matrix as
`gap: <reason>` for the narrower modes so the verification and render phases know what will not execute.

## Executable mode
- Every gated spec has `given caller`; every given/expected event has `for`, and so does every plain `produces <Name>` (inline `produces event` omits it and defaults to the identifier).
- Each read model has one key shared by its keyed `=> RM optional` queries; the identifier equals
  the projection's effective key (`cratis-screenplay-slice-design` `references/read-model-design.md`
  decision table). View specs assert through a keyed query.
- Run V3 (binding-ready, per `cratis-screenplay-toolchain`); binding-only codes (PLAY0273/0350/0352/0388/0389) appear only there.
- V4 runs through `screenplay test` or MCP `run-specifications`; record expected,
  discovered/selected and passed/failed/unsupported counts. Unbound/unsupported
  is not passed. Authoring-only refusal/redelivery/operation specs remain recorded
  contracts; test them at the owning target after authorized gap-fill.

## Renderable mode
- Specs use given events, a command `when`, and then events / readmodel / query / error /
  denied. No `when append` or `then no readmodel`. `given readmodel` appears only in the
  query-only lookup below.
- Query-only specs (no command `when`) are admitted by Stage v4.24.0 in exactly two cases (a v4.24.0 reading, not re-probed at 4.73.1), and
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
- A `then error` must name a constraint or rule that is actually rendered. Stage v4.24.0 (historical reading) admits
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
