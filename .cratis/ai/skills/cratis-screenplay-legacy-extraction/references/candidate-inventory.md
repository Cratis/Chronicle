<!-- cratis-ai-managed: skills/cratis-screenplay-legacy-extraction/references/candidate-inventory.md -->
# Candidate inventory (read-only analysis)

## Contents

- 1. Scope
- 2. Count
- 3. Slice status
- 4. Specification coverage
- 5. Structural gaps per slice
- 6. Orphans
- 7. Structural shapes
- 8. Report

Run this over the candidate folder (`.ai-work/screenplay/<model-slug>/candidate/` or the model
root) before handoff to review, and over any existing `.play` model you are asked to
characterize. It is read-only: it never edits the model and never writes review findings. It
summarizes what exists and where the visible gaps are; it is not the independent review
(`cratis-screenplay-model-review`). Adapted from Martin Dilger and Nebulit GmbH's analyze-existing-model, translated to
`.play` constructs.

## 1. Scope
Take an optional focus (one module or feature) from the brief; default is the whole candidate.
Read declarations through the Screenplay MCP model-inspection tools where the harness has them
(`cratis-screenplay-model-authoring`), else the files. Keep reads scoped. Record the source
identity and the date analyzed.

## 2. Count
Per module (one table each; `none` is a valid count):

| Element | Count |
|---|---|
| `event` | |
| `command` | |
| `readmodel` | |
| `query` | |
| `screen` | |
| `reaction` / `capture` | |
| `specification` | |

## 3. Slice status
Group slices by their recorded state: extracted, decided (keep / change / remove), undecided
with a question, blocked on an unknown. Flag a status that suggests stalled work: undecided with
no owner, or blocked with no question raised. Counts come from the evidence table, not from
guesswork.

## 4. Specification coverage
Per slice: at least one specification, or none. Report the share with and without, and list the
slices without (maximum 10, then "and N more"). Split the result by slice type when you can
tell it. A slice with only a success specification where a denial, validation or removal case is
evidenced is a coverage gap for `cratis-screenplay-scenario-coverage`, not a pass.

## 5. Structural gaps per slice
Determine each slice's type from what it contains and flag what is missing:

| Slice type | Expect | Flag if missing |
|---|---|---|
| State change | screen or trigger, `command`, `event` | any of the three |
| State view | `readmodel` with the builder that populates it (projection or `reducer`) or an evidenced performer-backed read, and a `query` or `screen` that reads it | either side |
| Automation | `reaction` (or trigger) and the `event` it produces or the command it invokes | either |
| Translation | `capture`, or a translator `reaction` over an imported or externally captured event, and the `event` it produces | either |

A read model may be built by a projection, by a `reducer`, or answered by a query `performer`;
follow the actual dependencies and do not call a reducer-backed or performer-backed read model a
gap just because it has no projection. Do not flag gaps that are clearly intentional (a slice described as internal with no screen).
Surface only gaps that look unintentional given the slice's name and its evidence rows.

## 6. Orphans
An `event` produced by no command, reaction or capture, appended by no specification and read by
no projection or reducer; a `readmodel` with no builder (projection, `reducer`) or evidenced
performer and no reader; a `command` no screen, form or trigger reaches. In legacy work an
orphan is usually unevidenced behavior or a lost origin:
list it with its evidence row. When V1 has no source errors, run
`--check field-origins,event-consumers,personas,purposes` (or `--check all`) on the candidate
folder (`PLAY0534` read-model field without an origin, `PLAY0536` event without a consumer; a
terminal or externally consumed event legitimately triggers it) and add each finding as an
inventory row. The checks report structure only; a finding is a prompt to look. Keep the manual
scan for what they do not cover. If the tooling cannot tell, say that this check was skipped.

## 7. Structural shapes
Thresholds and explanations adapted from Martin Dilger and Nebulit GmbH's core rules (see provenance). Check the wiring against four shapes. Internal shorthand only; in anything a business
stakeholder reads, describe the concern in plain words.
- **One screen, several commands** (the bed): a screen may legitimately offer several
  independently selected actions (an invoice list with RegisterInvoice and CancelInvoice). Report
  an instance only when one user action ambiguously executes several decisions, or the choice
  among them is made somewhere the model cannot see; state the domain consequence. Several
  separate buttons, each one command, are not a finding.
- **One command, more than two events** (left chair): candidate. Ask whether the outcomes always
  happen together or could happen independently.
- **One read model built from more than three events** (right chair): candidate. Ask whether the
  fields answer one question for one user.
- **One slice with noticeably more specifications than its neighbors** (shelf): candidate. Ask
  what the extra specifications cover.
Report a candidate only if it still looks like it does more than one job after you reason about
the evidence; a raw count over a threshold is not a finding.

## 8. Report
Never post anything outside the work area. Use this shape in the final report or in
`legacy/INVENTORY.md`:

```markdown
## Candidate inventory: <model> (source identity <id>, analyzed <date>)

### Modules (<n>): <Module>: <n> slices
### Slice status: decided <n> | undecided <n> | blocked <n>   Total: <n> slices
### Element counts (per module): table from section 2
### Specification coverage
- <n> of <total> slices have at least one specification (<pct>%)
- Without: <titles, max 10, then "and N more">
### Structural gaps: <slice title: what is missing> | none
### Orphans: <element: evidence row> | none | not checked (why)
### Structural shapes: <each action that ambiguously executes several decisions, with its domain consequence> + held-up candidates | none
### Summary
<2-4 sentences: maturity, the most important gap or risk, one concrete next step>
```

If a focus was named but not found, say so and list the modules that do exist. The summary names
no verdict (not "complete", not "equivalent"); verdicts belong to the review.
