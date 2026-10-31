<!-- cratis-ai-managed: skills/cratis-arc-command-execution/references/command-refusals.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Handle a returned command's refusal

Requires Arc 22.88.0 for `Invoke.Command` and refusal classification. Check the
`Cratis.Arc.Chronicle` package version; older returned commands fail the reactor
on any unsuccessful result.
Requires Arc 22.97.0 with Chronicle 19.63.0 for the synchronous `CommandInvocation` analyzer marker and ARCCHR0027 branch checks;
on older analyzers, use `Task<CommandInvocation>` and review branch results explicitly.

Import `Cratis.Arc.Chronicle.Reactors`. This handler fragment assumes registered
`WorkRequested` and `WorkRefused` events and a discoverable `StartWork` command:

```csharp
[OnceOnly]
public CommandInvocation Requested(WorkRequested @event) =>
    Invoke.Command(new StartWork(@event.Name))
        .OnRefused(Refused.ByConstraint("UniqueWork"), refusal => new WorkRefused(refusal.Message))
        .OnRefused(Refused.ByValidation, refusal => new WorkRefused(refusal.Message));
```

Use the actual constraint name. The first matching branch wins. Selectors are
`ByValidation`, `ByConstraint(name)` (exact, case-sensitive), `ByAnyConstraint`,
`ByAuthorization` and `Any`. **`Any` excludes authorization**; opt into handling
a denial explicitly rather than concealing a missing system actor.
`ExecuteCommandsAsSystem` still applies only to returned commands.

`OnRefused` returns a registered event, `EventForEventSourceId`, a stable nonempty
sequence of those, or `null` for acknowledgment. `Acknowledge(selector)` is the
explicit no-event form. Commands, operations, completions, unknown objects,
empty collections and invalid members are rejected before any branch event is
appended. ARCCHR0027 checks visible callbacks, not opaque helpers.

A handled refusal appends its branch events and **stops every remaining command
or invocation in that delivery**. Branch append failure fails the delivery;
no other refusal branch handles it. Collections mixing ordinary events and
invocations keep the events-first ordering. Earlier appends and successful
commands remain committed. No branch adds atomicity, deduplication or exactly-once
execution. If fan-out must continue independently after one duplicate, separate
its delivery/execution boundary rather than returning one invocation collection.

## Classify a result without hiding faults

`CommandResult.GetRefusal()` (import `Cratis.Arc.Commands`) returns a nullable
`CommandRefusal`: `Kind`, `Message`, `ConstraintName` and all `ConstraintNames`.
Authorization wins over constraints, which win over error-severity authored rules.
Named selectors match any violated name. Messages preserve authored text; do not
parse them to identify a rule.

Exceptions, authorization configuration failures, concurrency conflicts,
unavailable dependencies, malformed input and unknown or framework validation
reasons prevent classification even when a business refusal is also present.
`ValidationResultReason.FrameworkFailure` distinguishes framework-composed stream
completion errors from authored rules. Warnings/information alone do not classify
as refusals, including a warning that blocks under the command's severity policy.
These non-refusal failures and unhandled refusals fail the observer for recovery.

## Verify

Run `dotnet build -c Debug` and the affected specification project with
`dotnet test`. Cover selector ordering, each handled refusal, unchanged
remaining-command collaborators, branch append failure, and a concurrency or
exception failure that must not be acknowledged. Assert earlier committed effects
separately; a failed result does not prove rollback.
