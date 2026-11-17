<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/evidence-format.md -->
# Evidence format (reverse engineering)

Rows use the single shared schema in
[evidence-format.md](../../cratis-screenplay-event-modeling/references/evidence-format.md):
fields, source kinds, strength, merge rule and classification are defined there and not repeated
here. Prefix every id `RE-` (`RE-E1`) so it cannot collide with another lane's rows at merge. Write `evidence.jsonl` (and the `EVIDENCE.md` rendering) in `.ai-work/screenplay/<slug>/re/`;
the merge step of the
[understanding pipeline](../../cratis-screenplay-event-modeling/references/understanding-pipeline.md)
combines it with the legacy rows. For the richer table used with source code, see
`../../cratis-screenplay-legacy-extraction/references/evidence-table-template.md`.

## Locator notes for observed systems

- A walk row cites the observation id (`OBS-7`) and the redacted route, plus the screenshot file
  when one exists.
- A HAR row cites `har:<file>#<n>` and the route template (`POST /api/berths/reserve`) with field
  names, never values, cookies or tokens.
- An OpenAPI row cites `openapi:<operation>`; a binary row cites path plus offset, symbol or
  resource, and `RUN-<n>` for an approved run. A `binary` row from assembly reading is `Declared`.
- A person or document row cites the section or the person and date.

## Watermark

Record per feature and per source which state of the system the rows describe, in `STATE.md`
and, at acceptance, in the feature's `note`:

| Source | Watermark |
|---|---|
| source code, if offered | source identity (commit plus input digest, legacy extraction) |
| OpenAPI or docs | sha256 of the fetched file, its stated version and the fetch date |
| shipped binary or package | sha256, version string and build date |
| running app (walk, HAR) | version or build id the system shows, environment and capture date |

A source with no readable version gets its fetch date and the word `unversioned`. A later run
compares watermarks per feature before walking: unchanged means "unchanged since <watermark>"
and the feature is left alone; changed or `unversioned` means re-intake that feature only, with
stable ids (`process-drift.md`).

## Probe budget and table

A batch of API probes against one command counts as one L2 walk state, not one per case, so
the default walk budget does not forbid boundary sweeps. Keep one compact table per command
in `captures/` and cite its file from the rule rows:

| case | input | outcome | message |
|---|---|---|---|
| name empty | `""` | rejected | verbatim text |
| name 101 chars | 101 x | rejected | verbatim text |

## Example row

```json
{"id":"RE-E2","fact":"form rejected with \"Berth is already taken for these dates\"","sources":[{"kind":"walk","locator":"OBS-9, captures/screens/OBS-9.png","strength":"observed"}],"strength":"Observed","classification":"intent","unknowns":["is the check atomic under concurrent requests"],"candidate":"Booking/ReserveBerth","decision":"undecided"}
```

## Rules

- Every candidate element cites at least one evidence id; a slice with none is removed or sent
  back to intake.
- Cite ids in a slice `description` ("Evidence: RE-E1, RE-E2") and keep the detail in the table.
- Each candidate row carries a one-sentence `rationale`: why this evidence became this element
  and name ("two writes in one POST and a 409 on the second: one command with a guard, not two
  events"). At acceptance, promote the rationale with the evidence ids and the watermark into
  the slice `documentation` and the feature `note`, as legacy extraction does
  (`../../cratis-screenplay-legacy-extraction/references/re-entry-and-passes.md`); otherwise the
  evidence is lost when `.ai-work` is discarded.
