<!-- cratis-ai-managed: skills/cratis-screenplay-reverse-engineering/references/behavior-catalog.md -->
# Behavior catalog and scoring

## Contents

- What the catalog is
- Item format
- Statuses and the executed probe
- Class weights
- Naming fidelity is separate
- Leaked names
- The scorer script
- Hygiene

Names are easy to copy and say little about behavior. Score a rebuilt model on what the original
observably does (rules, rejections, flows, time rules) and report names separately. The catalog
needs an application you own, with its source or assemblies; the scorer builds it, never the
run under test.

## What the catalog is

A **ground-truth behavior catalog** is a list of observable given/when/then items extracted from
the ORIGINAL: validators, constraints, concept invariants, enum members, date rules,
authorization, state transitions and happy-path flows (command, event effect, query shows it).
Build it once from the original's source or assemblies, before reading the rebuilt model, and
freeze it: the same catalog scores the baseline run, every variant and every rerun.

A quirk is scored against what the original really does. An "original defect" item states the
observed behavior, not the intended one.

## Item format

One JSON file (`catalog.json`) with an optional `weights` header and an `items` array:

```json
{
  "weights": { "rule": 0.25, "rejection": 0.25, "flow": 0.25, "time": 0.1, "authorization": 0.1, "query-shape": 0.05 },
  "items": [
    {
      "id": "R-014", "class": "rejection", "area": "contacts",
      "given": "no contact exists", "when": "RegisterContact with neither first nor last name",
      "then": "rejected with 'A contact needs a first or a last name'",
      "weight": 1, "source": "assembly",
      "probe": { "method": "POST", "path": "/api/...", "body": {}, "expect": { "status": 400, "messageContains": "..." } },
      "status": "expressed", "executed": "not-run", "note": ""
    }
  ]
}
```

Each item is a behavior, not a name: "when a task is due 31 days ago and is not done then it is
listed as overdue"; "when a tag with an existing name is created then a constraint violation".
`class` is one of `rule`, `rejection`, `flow`, `time`, `authorization`, `query-shape`. `source`
is `assembly` when the item was found only through assembly reading; report that count on its
own. `probe` is the call that checks the item against a running app, or `null`.

## Statuses and the executed probe

Status in the rebuilt MODEL, read from the `.play`:

- `expressed` (1): equivalent behavior is stated (constraint, validate, specification, projection).
- `partial` (0.5): same rule with a wrong threshold or edge, or enforced only client-side.
- `missing` (0).

A different message wording does not make an item partial; a different threshold or condition does.

When the rebuilt app runs (`cratis run`), run each item's probe against it and set `executed` to
`pass`, `fail` or `not-run`. **Behavior (executed)** uses pass = 1 and fail = 0 where the probe
ran and the model status elsewhere; report it next to **Behavior (model)** and the `not-run`
count. An item `expressed` in the model but `fail` when executed is a platform gap or a model
bug: list it, never hide it.

## Class weights

The class split lives in one place, the catalog's `weights` header (it must sum to 1; the
default above applies when the header is absent). Per class the score is the item-weight mean;
item weights default to 1, and 1.5 for `time` (hardest to infer). A scorer may override an item
weight with a reason in `note`. Never change the split or the catalog after the first score is
seen.

Score a `time` item as a pair: the positive probe and the negative probe of the same rule must
both hold; an unfiltered list passing only the positive half is a fail. Performer-held time
rules run only in the rendered app (`cratis run --engine eventmodel`, or render), because the
semantic sandbox leaves performer bodies out.

## Naming fidelity is separate

Keep the inventory of names (commands, events, read models and queries, screens, constraints,
concepts, specifications, hand-written rules) in `naming.json`:

```json
{ "items": [ { "category": "commands", "name": "RegisterContact", "result": "exact", "leaked": true } ] }
```

`result` is `exact`, `renamed`, `partial` or `missing`; categories are `commands`, `events`,
`readModels`, `screens`, `constraints`, `concepts`, `specifications`, `handWrittenRules` with the
legacy weights .20, .15, .15, .15, .15, .10, .03, .07. Naming is never merged into behavior.
Report **Naming (non-leaked)** as the evidence of inference, and **Naming (all)** only with the
leak share.

## Leaked names

Mark a name `leaked` when the running original serves it verbatim (command names, fields, enums,
`x-concept`, route names), when an event name follows by past-tense convention from a leaked
command name, or when it comes from the `assembly` evidence class (assembly reading restates the
original). Leaked items are excluded from the numerator and the denominator of
Naming (non-leaked); the leak share (leaked / total) is reported beside it.

## The scorer script

Execute `scripts/score-fidelity.mjs` with Node.js:
`node scripts/score-fidelity.mjs catalog.json --naming naming.json [--json]`. The script does
only arithmetic and validation; building the catalog and marking statuses stay judgment.
Output: Behavior (model and executed) by class and in total with the `not-run` count,
**Structure (legacy)** (commands, events, read models, screens, concepts; `(exact + 0.5 x
partial) / items`, weights normalized over the categories present), **Rules (legacy)**
(constraints, hand-written rules, specifications), Naming (all), Naming (non-leaked) and leak
share. For legacy scores `renamed` counts as found; for naming it counts as not matching. Exit 0
prints the score block; exit 2 means an invalid catalog (unknown status, weights not summing to
1, a weighted class with no items) and prints why; `--self-test` checks the arithmetic.

## Hygiene

- The catalog is built from the original before the rebuilt model is read; the run under test
  never sees it.
- A catalog item found by the run only through an assembly is tagged `source: assembly` and
  reported in its own column.
- Scores stay in the lane's untracked `.ai-work/` folder and never enter the corpus.
