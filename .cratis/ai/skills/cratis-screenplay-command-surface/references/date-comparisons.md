<!-- cratis-ai-managed: skills/cratis-screenplay-command-surface/references/date-comparisons.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Chronological command date comparisons

## Contents

- Retention window and rejection specifications
- Missing-offset input rejection
- Clock operands and calendars (ESM v37)

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
`$context.occurred`), protected read paths, date rules on concepts and
reaction `where`. A refused whole comparison reports only PLAY0268, without
validating its literals. `today` and `now` are clock operands, admitted from v37 (see
[Clock operands and calendars](#clock-operands-and-calendars-esm-v37)).
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
      screen KeepWindow
        action KeepWindow for screen
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

## Clock operands and calendars (ESM v37)

Requires Screenplay 5.5.0 (ESM v37; `screenplay --version`); an older binder refuses these
forms with PLAY0268. A model that uses any of them selects v37 (decisions 0106, 0107, 0132):

- application root: `time zone "<id>"` or `time zone configurable default "<id>"`;
- application root: a `context` block of named `<member> time zone` members;
- command: `calendar "<id>"`, `calendar from $context.timeZone` or `calendar from $context.<member>`;
- specification: `given time zone "<id>"`;
- condition operands: offsets such as `today - 7 days`, `now + 2 hours` and `now - 30 minutes`,
  and `date of occurred` or `date of now`.

Admitted positions: command `validate`/`require` conditions, `produces when`, and the right
operand of a `validate` comparison rule on a `Date` or `DateTime` property. The other side is a
required scalar command property of the same primitive: `today` and `date of ...` are dates,
`now` is an instant. A `today` offset is whole days; use `now` for hours or minutes.
PLAY0268 still refuses a clock operand in a concept, reaction or screen, a clock compared with
another clock, and a `today` offset in hours or minutes. A bare `today` or `now` is a clock
operand unless a command property of that name shadows it, in which case it stays an ordinary
property path. PLAY0812-PLAY0814 report a repeated or malformed time zone, command calendar
and specification time zone; PLAY0852 an id that is not a known IANA zone (no UTC fallback);
PLAY0853 a calendar member that is neither `timeZone` nor declared; PLAY0854-PLAY0857 a
malformed `context` block, member type, repeated name or reserved name.

A command evaluates its clock in its calendar; without one, the application zone applies and an
application without a declared zone uses UTC, never the machine's zone. Use canonical,
exact-case IANA ids. A declared member is a calendar source only; it adds no `$context` value.
The host supplies each member's zone at run time and a specification supplies it with
`given time zone`; a missing, empty or unknown zone fails the command instead of falling back
to UTC. `given clock` fixes the instant, and a command a reaction invokes is evaluated in the
scenario zone too.

```screenplay
time zone configurable default "Europe/Oslo"
context
  businessTimeZone time zone
module Clocks
  feature Scheduling
    slice StateChange Schedule
      command ScheduleAppointment
        appointmentId String identifier
        occurred DateTime
        calendar from $context.timeZone
        produces when date of occurred <= date of now + 2 hours
          AppointmentScheduled
            for appointmentId
      event AppointmentScheduled
      screen ScheduleAppointment
        action ScheduleAppointment for screen
      specification InOslo
        given time zone "Europe/Oslo"
        given clock "2025-03-01T10:00:00Z"
        when ScheduleAppointment
          appointmentId = "a"
          occurred = "2025-03-01T11:00:00Z"
        then AppointmentScheduled
          for "a"
```

This adapts Screenplay's executable `Documentation/screenplay/fixtures/clock-preview.play`.
Extract and run it like the example above.

Executing command conditions that read the clock is not clock-trigger realization or rendering.
Stage admits the 37.0 pair with member scanning and refuses the clock per scope with
`STAGE-ESM-016`: its sandbox refuses each command that uses a calendar or clock operand and
each specification in a slice with a clock member (other commands run); an application time
zone or context member refuses every specification; and the renderer refuses the model. See
Stage `Documentation/reference/esm-feature-matrix.md`. Reaction `every`/`at` clock triggers are
a separate construct: the reference runner fires them for `when clock`, while Stage's live
Host and rendered applications refuse them. Preserve the rule and route delivery to
authorized gap-fill; see toolchain
[language admission](../../cratis-screenplay-toolchain/references/language-admission.md).
