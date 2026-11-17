<!-- cratis-ai-managed: skills/cratis-screenplay-specifications/references/date-comparisons.md -->
# Chronological command date comparisons

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`); older binders refuse
these comparisons with PLAY0268.

## What to cover

DateTime conditions compare UTC instants at tick precision: cover equal instants with
different offsets, increasing endpoints, an inverted interval and a one-tick boundary.
Every participating DateTime input without an offset or `Z` rejects with Validation
before rules, even when its comparison is skipped; add a missing-offset rejection, not
a clock fixture workaround.

## Matching and runtime cases

`then` matching stays ordinal: expected event values keep the exact produced text,
not an equivalent offset. Read the complete [retention-window specifications](../../cratis-screenplay-command-surface/references/date-comparisons.md)
for `require to >= from`, PLAY0268/0273 and Stage's v26 refusal. The missing-offset
runtime case needs an evaluator-request or target input test: putting the invalid
value in a typed `.play` fixture instead fails binding with PLAY0273; `then error`
does not exercise runtime validation.
