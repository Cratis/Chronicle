<!-- cratis-ai-managed: skills/cratis-arc-command/references/fresh-identifiers.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Fresh generated append targets

## Contents

- Direct writes and nested commands
- Opt out deliberately
- Specify the collision

Requires Arc 22.95.0 and Chronicle client 19.59.4 for guarded generated targets; deploy a Chronicle kernel 19.40.1 or later for retry-time emptiness checks. Check package references and the deployed kernel version before relying on the guard; older Arc needs an explicit whole-source `BeforeFirst` scope.

A `[GeneratedValue]` parameter defaults to `GeneratedValueFreshness.Guarded`.
When an Arc-owned append targets a validated value generated in that invocation,
it requires the **whole event source** to be empty, including other streams and
event types. Payload-only and response-only generated values add no scope.
Returned events, tuples, wrappers, `EventsWithConcurrencyScopes` and aggregates
acquired for that generated identifier are covered.

Requires Arc 22.100.0 or later and Chronicle client 19.65.2 or later for direct
unit-of-work writes and redacted enrollment-conflict translation. Check resolved
versions with `dotnet list package --include-transitive`; older Arc needs returned
events or an explicit whole-source `BeforeFirst` scope for direct writes.

An occupied destination returns `ValidationResultReason.ConcurrencyViolation`
with `ReasonDetail` `GeneratedIdentifierCollision`. Arc does not rerun `Handle()`.
A new invocation can generate another value, but first resolve an uncertain
commit: freshness is not idempotency. A fixed scenario pin remains fixed.

A compatible whole-source `BeforeFirst` scope merges with the guard. A conflicting
explicit scope, including `ConcurrencyScope.None` or a narrowed scope, fails with
`ConflictingConcurrencyScopes` before append. Without a transaction, keep each
generated source's returned events contiguous; an interleaved shape that would
split its guard fails with `GeneratedIdentifierEventsCannotBeSplit`.

## Direct writes and nested commands

The same guard covers these writes on the **command's actual unit of work**:

- `IUnitOfWork.AddEvent`, `AddEventWithNamedTags` and `AddEvents`.
- `eventLog.Transactional.Append`, `AppendMany`, `AppendWithNamedTags` and
  `AppendManyWithNamedTags`.

Returned, aggregate and direct events share one whole-source expectation. An
occupied generated destination rejects the whole pending unit, including events
for other sources, with the collision result above. Arbitrary new units and
external stores are outside this protection.

Nested pipeline invocations share the outer unit and leave completion to its
owner. Guarded captures from every active invocation participate, so a nested
handler's attribute-derived scope cannot narrow an outer generated guard.
Propagate nested rejection so the outer command rolls back earlier events.
An explicit aggregate `Commit()` can still complete the shared unit early.

After enrollment, the required scope survives disposal of the invocation's
contributor; later unscoped writes inherit it. An incompatible supplied scope
against that recorded generated guard maps to `ConflictingConcurrencyScopes`,
even if Chronicle rejects it before invoking contributors. Neither collision nor
scope-conflict messages expose the identifier or scope values. Arc uses typed
scope properties and recorded guard provenance, not message parsing; unrelated
enrollment conflicts retain Chronicle's redacted `ConflictingEnrollmentScope`.
Scope-only `AddEvents` entries preserve existing requirements but do not invoke
contributors or establish a new generated guard. A completed nested invocation's
untargeted capture leaves no enrolled requirement.

Custom units must support enrollment contributors. A guarded generated parameter
fails closed with `FreshIdentifierGuardRequiresEnrollmentContributors` if the unit
cannot register one; no generated parameters or only `Unguarded` parameters do not
require that support.

`AddEvents` checks all destinations before staging that call; a scope conflict
stages none of its events. Transactional `AppendMany` and
`AppendManyWithNamedTags` stage per event, so a later rejection can leave earlier
events pending. Let the exception escape `Handle()` to roll back the unit;
catching it and succeeding can commit those earlier events.

Immediate `eventLog.Append`, `AppendMany` and their named-tag variants bypass
enrollment, cannot be rolled back and receive **no automatic freshness guard**.
Prefer returning events. An integration needing direct writes must use the
command's transactional unit; an immediate caller needs an explicit whole-source
`BeforeFirst` scope and must inspect the append result, without automatic
`GeneratedIdentifierCollision` classification.

## Opt out deliberately

Only when appending to an existing generated destination is intentional, use this
parameter attribute fragment (imports `Cratis.Arc.Commands.ModelBound` and
`Cratis.Arc.Commands`):

```csharp
[GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)]
```

The opt-out retains generation, validation and capture, not automatic freshness.
Client-minted and natural identifiers are outside this generated-parameter policy;
use an appropriate constraint or explicit concurrency scope for them.

## Specify the collision

Use `new CommandScenario<TCommand>()`; it selects the in-process log from the
command's read-protection mode and dependencies before seeding. If a seeded
collision throws `GeneratedIdentifierCollisionRequiresDecisionReads`, the
scenario stayed in legacy mode: call the compatibility switch `UseDecisionReads()`
before seeding. Then `PinGeneratedValue(name, id)` and `Given.ForEventSource(id).Events(existingEvent)`.
Assert the concurrency reason and collision detail, unchanged append tail and
one handler execution. Also cover an empty source receiving two events through
the chosen write API, an occupied source whose events belong to another stream,
mixed returned/direct writes, and a nested rejection rolling back the outer unit.

Requires Arc 22.95.1 for `GeneratedIdentifierCollisionRequiresDecisionReads`:
default scenario mode fails fast when given events and a guarded pin share an id.
Older default mode silently leaves the append log empty; it cannot prove occupancy.
Run `dotnet build -c Debug` and the affected specification project with
`dotnet test`; all collision and empty-source facts must pass.
