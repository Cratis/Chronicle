<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/cases-to-specify.md -->
# Cases to specify

Decide each case's expected outcome with the domain expert, then hand the list to
`cratis-screenplay-scenario-coverage` for the coverage matrix. A case may be "not applicable" only with a domain reason. A case
whose outcome nobody knows is an open question in `STATE.md`, not a spec.

Enumerate per reaction trigger, capture append branch and invoked command. Give each
applicable case a named outcome and a spec location, or an explicit capability/decision gap.
One spec in the slice does not cover the others.

## Automations
| Case | Question | Typical Screenplay shape |
|---|---|---|
| happy path | trigger occurs, work done | `when append <Trigger>` -> `then <Result>` (Automation slice) |
| condition false | trigger occurs, nothing should happen | assert a view (`then no readmodel …`), not an empty event list |
| queue opens / closes | item appears, item disappears | `then query …ById`; `when append <Closing>` + `then no readmodel … for` |
| already done | trigger re-delivered after the result exists | `given <Result>` + `when <Command>` -> `then error "<fixed constraint message>"` |
| invoked command rejects | validation or constraint refuses | command spec in its StateChange slice; record what the queue shows afterwards |
| no declared actor | invoked command is gated | specify its denial; for the trusted path declare reaction `runs as system role` and reference-test with a standalone compiler that admits v10 |
| clock boundary | due exactly at the instant, just before, just after | `given clock` + `when clock`; boundary semantics are contested in the docs, mark the expectation as an assumption |
| each branch | does every filter and conditional production have both outcomes? | concrete triggering and non-triggering values; assert the resulting facts or view |
| denied actor | every command and query under an inherited module/feature `authorize` needs its own denial; also, can an unauthorized caller reach a gated invoked command by another origin? | command spec with otherwise-valid inputs, `given caller` carrying the wrong role/claim (or no caller) and `then denied`; separate from the caller-less reaction gap |
| competing claim | two sources claim the same constrained value | `given` the first fact, `when <Command>` the second, `then error "<message>"` |
| all closing paths | result, cancellation or supersession ends work | one `remove with` path per case, `then no readmodel … for` |
| distinct later work | same entity needs another legitimate job | prove dedup does not suppress the new occurrence |
| partial cascade | earlier effect succeeded, later invocation failed | record which facts persist and how retry resumes; no cascade rollback |
| loop / runaway | result re-triggers the reaction | prove termination in text; no spec can show "never" |
| external effect fails / times out | send failed, or unknown | item stays pending; retry sweep; see `effects-and-idempotency.md` |

## Translations
| Case | Question | Typical shape |
|---|---|---|
| first sight | new record key arrives | record `when capture`, `when added` append -> `then <Fact>` and any separate Automation follow-up |
| update | a field changes for a known key | `given capture` same key's previous record + `when capture`; field-change append holds |
| consumed public event | capture has `source events` | `given <FromEvent>` / `when append <FromEvent>`, never capture-record fixtures (PLAY0701) |
| arrives before our entity | the reference is unknown to us | decide: park, reject, or record an orphan fact for follow-up |
| duplicate delivery | same record again | `given capture` (same record) + `when capture`: nothing new; or constraint rejection on our fact |
| out of order | an older record after a newer one | `given capture` newer + `when capture` older; expected outcome stated |
| partial | required field missing | decide: enrich later, pending fact, or reject; never a fabricated default |
| invalid | unknown code, wrong type | `translate` has no entry -> decide and specify |
| changed / corrected | outside value changes after our fact | correction fact or open decision (`translation-patterns.md` §6) |
| mis-correlated | reference points at the wrong stream | property constraint on the external reference, or an expert review case |
| personal data | field we must not keep | excluded from the append mappings; record the reason in `documentation`; the source record may still carry it |
| authenticity / ownership | wrong source, tenant or entity reference | specify the rejection and name where it is enforced; capture metadata does not verify it |
| each mapping branch | every recognized code has its intended local meaning | concrete payload and exact translated values; unknown code is a separate case |
| same object, different occurrence | two legitimate events share a provider object id | both stay representable; dedup hits only the repeated occurrence |
| material field changes alone | amount or score changes, status does not | explicit correction or rejection outcome; inspect the append conditions |
| units and timestamps | units, precision, timezone, provider time differ | concrete conversions; record unsupported transforms |
| deferred recovery | correlation or enrichment arrives after the record | owner, resumption point and stable work identity |
| consent missing | processing needs a permission not given | no unauthorized domain effect; name where enforced |
| malformed record | unparseable or unmappable input | a recorded failure fact or dead-letter view, not a throw |

## Concurrency and retries (both kinds)
- Two deliveries of the same logical occurrence at once: which atomic mechanism prevents
  duplicate work, and does the caller see success or rejection? Also two distinct legitimate
  occurrences, which must not be mistaken for retries.
- The append commits but the acknowledgement to the source is lost: the source resends; layer
  1 or 2 dedup must absorb it.
- Projection lag: the reaction's `reads` sees the queue before or after the closing fact?
  The decision must not depend on it (it is not protected).

## Spec limits (tool differences: `cratis-screenplay-toolchain` `SKILL.md`)
- Current admitted Automation/Translate specifications run through standalone
  `screenplay test` or MCP `run-specifications`. Validate/render alone is not V4;
  report unsupported/unbound separately from passing scenarios.
- A command spec lists the cascade (v6 semantics: after
  `when <Command>`, `then` lists every new fact, including reaction appends). Only when the V1
  tool's compiler stops before v6 does it report a false PLAY0285 on a cascade:
  then keep the cascade out of the command spec, specify it in the Automation slice
  (`when append <Trigger>`) and record the expectation. See `cratis-screenplay-scenario-coverage`.
- `given capture` never persists between specs: state the baseline in each record
  update spec. First records (root, child, nested) are added only; field-change
  `and`/`or` and transitions need previous state. Expressions/maps/unguarded record
  appends are unchanged. See captures-and-reactions `references/record-capture-specifications.md`.
- For `source events`, use prior typed events instead of `given capture`; cover
  first added, per-key overlays, unchanged input, independent keys and optional
  foreign `for` unrelated to the content key. Reference execution does not prove
  capture redelivery deduplication or observer ordering.
