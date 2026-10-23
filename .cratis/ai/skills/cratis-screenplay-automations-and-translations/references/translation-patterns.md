<!-- cratis-ai-managed: skills/cratis-screenplay-automations-and-translations/references/translation-patterns.md -->
# Translation patterns

## Contents

- 1. Where outside facts may enter
- 2. Correlation first
- 3. Two vocabularies
- 4. Per-field disposition
- 5. Dedup and ordering
- 6. Corrections from outside
- 7. Specifying translations

Complete compiled example: `translate-example.md` (binds as ESM v6; a compiler that stops before v6
compiles it but rejects the slice at binding).

## 1. Where outside facts may enter
Only two routes, both in a `Translate` slice:
- **`capture`** over a source (api poll, webhook, message topic): key, map, conditional
  appends. Change detection compares each record with the last one seen for the same key.
- **translator `reaction`** over a foreign public event declared with its origin.
  Opaque unquoted imports do not bind. A reaction to a local event a capture
  appended is a follow-up Automation in a separate slice, not another translation.

A directionless Translate with an unrouted reaction to its own capture's appends
(including children/nested) warns PLAY0694 and fails `--warnaserror`; explicitly
inbound local-event consumption is PLAY0616. Keep the capture in Translate and
choose a separate Automation slice and its specs; there is no automatic repair.
Requires Screenplay 4.176.0 or later (`screenplay --version`) for PLAY0694; older compilers leave the directionless shape unchecked.
Not a translation: our own event-to-event follow-up (Automation), infrastructure plumbing,
and raw database row changes published as-is.

## 2. Correlation first
Before mapping fields, answer: how does an outside record find *our* stream?
| Situation | Approach |
|---|---|
| we start the outside interaction | allocate our id first and send it out as their reference; their reply echoes it; capture `key` = that field (`translate-example.md`: `reference`) |
| they start it and carry our id | key on it; validate it exists (an unknown id is a case to specify) |
| they only carry their own id | record their id on our fact when we first learn it, and keep a lookup view from their id to ours; the record that arrives before the link exists is a case to specify |
Never make an outside id our stream identity unless the business genuinely identifies the
thing by it. Keep it as a property when it is needed to correlate or deduplicate.

## 3. Two vocabularies
- The capture appends an **external fact** in the provider's terms (`ProctorResultReceived`
  with a translated `outcome`).
- Reactions to that local fact record further **our facts** (`ExamPassed`, `ExamFailed`)
  in a separate Automation slice, each guarded by our constraints.
- A further worker needs a distinct local decision, obligation or external effect; otherwise
  project views from our fact directly.
- Name our facts for what they mean to us, not for the transport (`<X>Synced`,
  `<X>WebhookReceived` are smells; a business fact such as `PaymentReceived` is fine).

## 4. Per-field disposition
For every field of the outside record, decide and write in slice `documentation`:
| Disposition | Meaning | Screenplay |
|---|---|---|
| **map** | carried into our fact as is or translated | `map` + mapping `x = $.x`; `translate` for codes |
| **enrich** | we add data the record lacks, from our own facts | later decision in a command or view; not in the capture |
| **infer** | derived from the record by a rule the business confirms | `map` template or a translator condition |
| **ignore** | not needed, or not permitted for this purpose | keep it out of appended event payloads; define separately what source/last-seen records the target retains |
Missing data our fact needs: either enrich from our side, or the fact is not ready yet
(model a pending fact plus a case to specify), never invent defaults.
See "Capture data-protection traps" in SKILL.md.
Malformed or unmappable input becomes a recorded failure fact (or a dead-letter view), never
a dropped record or a thrown error, which would pause the observer partition.

## 5. Dedup and ordering
Two layers, each with its own scope:
1. **Capture change detection** - per `key`, against the last record seen. An unchanged
   redelivery appends nothing. Where the last-seen record is stored and how long it survives
   is the target's business; specs supply it explicitly with `given capture` and never persist
   it.
2. **Constraint on our fact** - `unique event` per stream (one result per attempt), or
   `unique externalRef on OurFact` when the same outside reference must never land on two of
   our streams. A property constraint does **not** stop the same stream receiving the same
   value twice; add `unique event` for that.
Out-of-order records: decide whether an older record may overwrite a newer one. Use
`when field from <v> to <v>` for transitions that only make sense in one direction, and
specify the reverse arrival.
A rejection by our constraint inside a capture stops that record's processing in the
reference; what the target does with the source record (acknowledge, retry, park) is a
realization decision to record.

Distinguish the provider's object id, its event/delivery id and its revision. One object can
produce several legitimate events (charge, refund); define which repetition is a duplicate
and its retention window before choosing constraints. `unique event` fits only when the
business permits that event once per source. Change detection evaluates only the fields in
the append conditions: a redelivery with the same outcome but a changed score or amount is
not automatically a duplicate; decide whether it is a correction, deferred work or a
rejection, and specify it.

## 6. Corrections from outside
When the outside system changes its mind (re-grade, refund, cancelled delivery), the change
is a new fact for us (`ResultCorrected`), never an overwrite. Mutually exclusive
`unique event` lines will refuse a second result; decide with the expert whether a correction
event with `released by` or a separate correction flow is right.

## 7. Specifying translations
- Record sources: `when capture <Capture>` with source fields -> `then` every new
  fact, including follow-up Automation appends. A first record of a key is added
  only: use a separate `when added` append for first sight, keeping the field guard
  for updates. Seed updates with `given capture` of the same key. Child/nested
  additions follow the same rule; expressions, maps and unguarded appends are unchanged.
  Requires Screenplay 4.175.0 or later (`screenplay --version`); older evaluators
  treat first-record fields as changed.
- Event sources: `given <FromEvent>` / `when append <FromEvent>`, never capture
  records (PLAY0701). The key is required on every source event (PLAY0700).
  Read [public event captures](public-events-and-direction.md) for executable examples,
  overlay semantics and their prerequisite.
- `when append <LocalFact>` -> `then <OurFact>` tests the follow-up Automation alone.
- Run admitted cases with `screenplay test <model>`; all selected specs must pass.
  Case list: `cases-to-specify.md`.
