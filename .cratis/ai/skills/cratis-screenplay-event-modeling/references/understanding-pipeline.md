<!-- cratis-ai-managed: skills/cratis-screenplay-event-modeling/references/understanding-pipeline.md -->
# Understanding an existing system: one pipeline

Starting point when the inputs are any mix of source code, a running application, network
captures, API descriptions, documents or Prologue output. It routes each input to the owning
skill and merges the results into one model with one evidence file. It only routes: approvals,
hosted-model disclosure and Prologue rules are those of the owning skill and
[safety.md](../../cratis-screenplay-modeling-lifecycle/references/safety.md).

## Contents

- Intake table
- Procedure
- Safety
- Report

## Intake table

| Input | Owning skill | Contributes |
|---|---|---|
| Running system or UI walk, HAR, OpenAPI, product docs, binary or package | `cratis-screenplay-reverse-engineering` | `walk`, `har`, `openapi`, `doc`, `binary` rows |
| Source code, `cratis screenplay generate` output, tests | `cratis-screenplay-legacy-extraction` | `code`, `test`, `generate` rows, loss report |
| Prologue capture or output | `cratis-screenplay-legacy-extraction` ([prologue-safety.md](../../cratis-screenplay-legacy-extraction/references/prologue-safety.md)) | `prologue` rows |
| Chronicle event store | `cratis-screenplay-legacy-extraction` ([chronicle-observations.md](../../cratis-screenplay-legacy-extraction/references/chronicle-observations.md)) | `runtime` rows |
| Nothing yet, or only interviews | `cratis-screenplay-discovery` | no rows; this pipeline does not apply |

## Procedure

1. **Inventory the inputs.** List each one and record whether it is owned, permitted or
   public-only. A model built from public-only material is labeled plausible in the model notes
   and the final report, never faithful.
2. **Run each owning skill's evidence levels** in its own working folder, in the order reverse
   engineering gives: the running system first, then the executable, source last. Each skill
   writes the schema of [evidence-format.md](evidence-format.md) with its own lane id prefix
   (`RE-`, `LX-`, `PR-`), so local ids never collide.
3. **Merge** into `.ai-work/screenplay/<slug>/evidence.jsonl` (and the `EVIDENCE.md` rendering) by
   the merge rule of the schema: one row per claim with every source it has, strength recomputed,
   disagreements kept as two rows (`conflicts with <id>`). This is one row per fact, not one row
   per source; a merged row keeps one lane id and lists the others in `aliases`.
4. **One inventory, one candidate.** Build one L0 inventory and one candidate model from the
   merged rows. Run P3 specification coverage on that merged candidate (derive specifications
   from the evidenced rules and refusals), then continue with the lifecycle phases P4-P9
   (`cratis-screenplay-modeling-lifecycle`); P4 expects a model with specifications. Every rule and refusal row shows which kinds back it;
   a rule with a single kind is listed as single-source.

## Safety

- This reference never starts Prologue and never runs an executable. Approvals, hosted-model
  disclosure and Prologue rules belong to the owning skill and to
  [safety.md](../../cratis-screenplay-modeling-lifecycle/references/safety.md); follow them there.
- Unattended: Prologue is never started. List the proposed commands and use captures that
  already exist.
- Captures stay in the working folders and are never committed.

## Report

Name, per input, the owning skill that ran or "not run: <reason>"; the count of merged rows and
of single-source rules; the open conflicts; and whether the result is faithful (owned or
permitted inputs with source) or plausible (public material only).
