<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/date-comparisons.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronological command date comparisons

## Contents

- Retention window and rejection specifications
- Missing-offset input rejection

Requires Screenplay 4.199.0 (ESM v26; `screenplay --version`); older binders refuse date comparisons with PLAY0268. Upgrade the compiler without weakening the rule.

Use `==`, `!=`, `>`, `>=`, `<` or `<=` with matching primitives: Date/Date or
DateTime/DateTime. A concept may type a command property; date rules on the concept
itself remain refused. These are the admitted positions:

| Position | Left | Right |
| --- | --- | --- |
| `require`, `produces when` | Required scalar command property | Required scalar command property of the same primitive, or literal |
| Command/property validation rule | Subject property | Literal only |

`Date` orders calendar dates without a time zone. `DateTime` compares UTC instants
at tick precision (seven fractional digits), including `==`/`!=`: different offsets
can name equal instants. Literal operands bind to canonical text (`yyyy-MM-dd`, or
UTC `yyyy-MM-ddTHH:mm:ss.fffffffZ`). An unparsable date literal or an offset-less
DateTime literal in an admitted position reports PLAY0273.

Every participating DateTime input must carry an offset or `Z`. Otherwise input
validation rejects the command with Validation **before any rule or condition**,
even if the comparison would short-circuit or its production would be skipped.
Targets must enforce this before deserialization attaches a local offset, and
preserve tick precision or refuse the comparison; JavaScript `Date` alone truncates
precision. Client validators omit date ordering rules. Optional absent values still
pass validation rules other than `not empty`; optional condition operands refuse.
A .NET host that executes commands itself can reuse the reference evaluator's date semantics (Requires Screenplay 4.203.0): see [date comparison checks](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/date-comparison-checks.md) for `SemanticDateComparisons` and `SemanticConditionEvaluator`.

PLAY0268 still refuses mixed Date/DateTime, `$context` operands (including
`$context.occurred`), `today`, protected read paths, date rules on concepts and
reaction `where`. A refused whole comparison reports only PLAY0268, without
validating its literals. A `given clock` does not admit `today` or now.
Requires Stage 4.135.0 for v26 admission; older Stage refuses v26 with STAGE-ESM-016.
The semantic Host and specification executor run the comparisons, including through
concepts, and reject an offset-less DateTime input after authorization and before any
rule. The renderer, legacy engines and default sandbox engine refuse them with
STAGE-ESM-046, naming the property, and emit no artifacts or date validators. Standalone
binding/reference execution is not rendering: preserve the rule and route delivery to
authorized gap-fill.

## Retention window and rejection specifications

Two required DateTime inputs express a caller-chosen interval, not the server's
clock. Keep state-dependent retention-policy reads separately protected; this
example models only interval ordering. `then` matching remains ordinal, as do
keys, constraints and freshness: preserve the exact produced payload spelling,
not a different offset that happens to name the same instant.

```screenplay
concept WindowId : Uuid
module Retention
  feature Windows
    slice StateChange KeepWindow
      command KeepWindow
        windowId WindowId identifier
        from DateTime
        to DateTime
        validate
          require to >= from
            message "The window must not end before it starts"
        produces event WindowKept
          from DateTime = from
          to DateTime = to
      specification AcceptingEqualInstantsWithDifferentOffsets
        when KeepWindow
          windowId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          from = "2026-10-01T10:00:00+02:00"
          to = "2026-10-01T08:00:00Z"
        then WindowKept
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          from = "2026-10-01T10:00:00+02:00"
          to = "2026-10-01T08:00:00Z"
      specification RejectingAnInvertedWindowByOneTick
        when KeepWindow
          windowId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          from = "2026-10-01T08:00:00.0000002Z"
          to = "2026-10-01T08:00:00.0000001Z"
        then error "The window must not end before it starts"
      specification AcceptingAnIncreasingWindow
        when KeepWindow
          windowId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          from = "2026-10-01T08:00:00Z"
          to = "2026-10-01T09:00:00Z"
        then WindowKept
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          from = "2026-10-01T08:00:00Z"
          to = "2026-10-01T09:00:00Z"
```

Extract the complete fence into an ignored `.ai-work/` model folder. Run
`screenplay <folder> --warnaserror --no-color` (zero errors/warnings), then
`screenplay test <folder>` (all three specifications passed, none Unsupported).

## Missing-offset input rejection

Add a `RejectingAMissingOffset` test at the evaluator-request or target input
boundary: submit `from = "2026-10-01T08:00:00"` with a valid `to`, then reverse which
input lacks an offset. Expect Validation before any rule, no facts, and the message
`DateTime command property 'from' requires an explicit offset or Z.` (or `'to'`).
Include a skipped-condition case where applicable. A typed `.play` fixture with
that invalid value refuses binding with PLAY0273; `then error` does not turn it
into an executed runtime rejection. Record the target test separately, not as a
passing `.play` specification or a `given clock` workaround.
