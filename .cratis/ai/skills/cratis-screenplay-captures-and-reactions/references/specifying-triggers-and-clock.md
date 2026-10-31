<!-- cratis-ai-managed: skills/cratis-screenplay-captures-and-reactions/references/specifying-triggers-and-clock.md -->
# Specifying triggers and the clock

**Specify what sets them off** (execution from ESM v6): `when clock "<instant>"`
for a reaction on `every` or `at`, `when trigger <Trigger>` with its values for one
on an application trigger, and `given capture` / `when capture <Capture>` with the
source record's fields for a record capture, then the events that should follow.
For `source events`, use `given <FromEvent>` / `when append <FromEvent>` instead
(PLAY0701). First sight is `added` only; update guards need the same key's prior
record or events. See [record capture cases](record-capture-specifications.md) and
[public event captures](../../cratis-screenplay-automations-and-translations/references/public-events-and-direction.md).
`given clock` fixes the occurrence time, so values mapped from `$context.occurred`
can be asserted. Excerpt of the complete example in [complete-examples.md](complete-examples.md):

```screenplay excerpt
specification IssuingTheWeeklyDigest
  given clock "2026-10-05T07:00:00Z"
  when clock "2026-10-05T07:30:00Z"
  then DigestIssued
    for "weekly"
    issuedAt = "2026-10-05T07:30:00Z"
```

A compiler that stops before ESM v6 parses these actions and checks them against the application
but binds them to nothing (`PLAY0268` names ESM v6).
The standalone tool (toolchain `SKILL.md`) executes admitted scenarios through `screenplay test`
or MCP `run-specifications`; source compilation alone is authored evidence.
Clock rules: the clock is UTC and exact; an
occurrence fires once when it is **due after `given clock` and at or before
`when clock`**, in time order; intervals count from the Unix epoch; `when clock`
needs `given clock`, so equal instants fire nothing. Limits: 10,000 occurrences per
advance, 1,000 new facts per scenario. Spec grammar and outcome comparison:
`cratis-screenplay-specifications`.
