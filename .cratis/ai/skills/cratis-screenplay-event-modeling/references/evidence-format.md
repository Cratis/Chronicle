<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/evidence-format.md -->
# Evidence format

## Contents

- Purpose
- Files
- Row fields
- Ids
- Source kinds and locators
- Strength
- Merge rule
- Classification
- Unknowns
- Table example
- JSON lines example
- Rules

## Purpose

The single definition of the evidence schema. Reverse engineering, legacy extraction and Prologue
rows all use it; no other file copies its fields or strength rules.

## Files

- `evidence.jsonl` is canonical: one JSON object per line. `EVIDENCE.md` is its markdown table
  rendering, for people.
- Each skill writes the same schema in its own working folder (`.ai-work/screenplay/<slug>/legacy/`,
  `.ai-work/screenplay/<slug>/re/`). The merge step of
  [understanding-pipeline.md](understanding-pipeline.md) writes the merged files at the slug root,
  `.ai-work/screenplay/<slug>/evidence.jsonl` and `EVIDENCE.md`.
- An older file with a flat `sourceKind`, `locator` and `strength` per row is valid: read it as a
  row whose `sources` has one element.

## Row fields

A row is one fact about the system, keyed by `candidate` plus a normalized claim, not by source.

| Field | Content |
|---|---|
| `id` | lane-prefixed, stable, never renumbered, unique across the merged file (see Ids) |
| `fact` | what was seen, read or said, in neutral words, no intent |
| `sources` | array of `{kind, locator, strength, capturedAt?, detail?}`, one entry per independent observation |
| `strength` | the row level, derived from `sources` (see Strength) |
| `classification` | see Classification |
| `unknowns` | what this row does not establish, as a question; `none` only when true |
| `candidate` | declaration it feeds (`Booking/ReserveBerth`, event `BerthReserved`) or `none`; proposed, never evidence |
| `decision` | `undecided`, `keep`, `change` or `remove` |
| `decidedBy` | user, expert name or `proposed` |
| `verdict` | optional expert verdict: `confirmed`, `corrected`, `rejected`, `not asked` |
| `aliases` | optional lane ids folded into this row by a merge |
| `notes` | links to other rows, open question ids, `conflicts with <id>` |

Optional extension fields for source-backed work: `technicalShape` (method and route, tables,
status-code mix), `generatorDisposition` (`Lowered`, `ProvenanceOnly`, `OmittedWithDiagnostic`,
`Conflicted`, `n/a`). Evidence and decisions stay separate: `fact` through `unknowns` are as-is,
`candidate` through `verdict` are target.

## Ids

Each working folder prefixes its ids with its lane: `RE-` reverse engineering, `LX-` legacy
extraction, `PR-` Prologue, `DOC-` documents, `DISC-` discovery (`RE-E1`, `LX-E1`). Local numbering
then cannot collide when folders are merged, so an id is unique across the merged file. A merged
row keeps one lane id as `id` and lists the others in `aliases`; ids are never renumbered, and a
retired id stays resolvable through `aliases` or a note. Cite the full prefixed id in
`conflicts with`, slice descriptions and reports. An unprefixed older id (`E1`) is read as local to
its folder and prefixed when merged.

## Source kinds and locators

| `kind` | Locator |
|---|---|
| `walk` | observation id (`OBS-7`) plus redacted UI route; screenshot file when one exists |
| `har` | `har:<file>#<n>` with the redacted route template |
| `openapi` | `openapi:<operation>` |
| `doc` | document section and version |
| `binary` | path plus offset, symbol or resource; process-run id (`RUN-3`) for an approved run |
| `code` | inspected repository-relative `path:start-end`, the file name exactly as given, bound to the source identity; `not found` keeps the claim `unknown` |
| `test` | the test method's `path:start-end` |
| `generate` | generated declaration address plus diagnostic id |
| `prologue` | capture file plus record |
| `runtime` | Chronicle target, event store, namespace, schema identity and generation or bounded event range, observation time |
| `expert` | person and date |

Never substitute one kind's locator for another's. Keep locators redacted: route templates and
field names, never values, tokens or personal data.

## Strength

Per source: `observed` (walk, har, prologue, runtime), `declared` (openapi, doc), `read` (code,
test, generate) or `stated` (expert, or a person's statement in a doc). For a `generate` or `code`
source keep the generator ladder, `Exact`, `Configured`, `Conventional` or `Heuristic`, in `detail`.

Row level (`Confirmed`, `Observed`, `Declared`, `Read`, `Stated`, `Inferred`):

| Strength | Rule |
|---|---|
| `Confirmed` | two or more independent source kinds agree (a capture plus code; a doc plus a walk) |
| `Observed`, `Declared`, `Read`, `Stated` | one source kind, named by its per-source strength |
| `Inferred` | a reading of other rows; always cites them in `notes` |

Two captures are one kind. A rule seen in a capture, stated in docs and found in code is one row
with three `sources` and `Confirmed`.

## Merge rule

Rows from different working folders that make the same claim about the same candidate become one
row: union the `sources`, recompute `strength`, keep one lane id as `id` and list the
others in `aliases`. A disagreement is never merged: keep two rows, set `notes` to `conflicts with <id>` on
both and classification `unknown`. A row with a single source kind is single-source; the report
lists it as such.

## Classification

`intent`, `consequence`, `infrastructure`, `noise` or `unknown`. A write with no visible user
command behind it is a `consequence` with a subclass: `maintenance`, `correction`, `integration`,
`scheduled` (only with an evidenced trigger) or `unexplained`; never assume an automation.

## Unknowns

- Every row carries its unknowns. The common ones: the rule behind a rejection, who is allowed,
  whether the call repeats safely, what else changed, what a different role sees.
- Each unknown that blocks modeling becomes a question with an owner and an interim assumption,
  recorded as an anchored `question` note on the candidate.
- Never fill a gap by guessing and then citing the guess as evidence. A gap you could not close
  stays `unknown` in the model's notes and in the final report.

## Table example

| id | fact | sources | strength | classification | unknowns | candidate | decision |
|---|---|---|---|---|---|---|---|
| RE-E1 | `POST /api/berths/reserve` returns 201 and the reservation shows in the list; a berth already taken for the dates is refused | har `har:booking.har#12`; code `BerthService.cs` lines 40 to 58 | Confirmed | intent | who may reserve for another member? | `ReserveBerth` -> `BerthReserved` | undecided |
| RE-E2 | form rejected with "Berth is already taken for these dates" | walk `OBS-9` | Observed | intent (rule) | is the check atomic under concurrent requests? | refusal on `ReserveBerth` | undecided |

## JSON lines example

```json
{"id":"RE-E1","fact":"POST /api/berths/reserve returns 201; a berth already taken for the dates is refused","sources":[{"kind":"har","locator":"har:booking.har#12","strength":"observed"},{"kind":"code","locator":"BerthService.cs:40-58","strength":"read","detail":"Exact"}],"strength":"Confirmed","classification":"intent","unknowns":["who may reserve for another member"],"candidate":"Booking/ReserveBerth","decision":"undecided","decidedBy":"proposed","aliases":["LX-E7"]}
```

## Rules

- Every candidate element cites at least one evidence id; a slice with none is removed or sent
  back to intake.
- A rule or refusal row shows which kinds back it; a single-kind rule is listed as single-source.
- Add rows as you go; do not rewrite ids when rows are split or merged, retire them with a note.
- Cite ids in a slice `description` ("Evidence: RE-E1, LX-E7") and keep the detail in the table.
