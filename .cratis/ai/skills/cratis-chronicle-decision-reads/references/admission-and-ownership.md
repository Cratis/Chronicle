<!-- cratis-ai-managed: skills/cratis-chronicle-decision-reads/references/admission-and-ownership.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Admission and commit ownership

Source: `Cratis/Chronicle`:
`Source/Clients/DotNET/ReadModels/DecisionReads.cs`, `DecisionReadRefusalReason.cs`,
`Source/Clients/DotNET/Transactions/UnitOfWork.cs`, `DecisionReadCommitOwner.cs`,
and `UnitOfWorkLifecyclePolicy.cs`.

## Why a shape is refused

Admission requires exactly one supported projection, or an unfiltered dependency-free reducer, on the event log. It is not
an adapter for arbitrary read-model storage:

| Reason | Shape to inspect |
| --- | --- |
| Reducer / AmbiguousProjection / NotEventLog | a model with no admitted definition, several projections, or another sequence |
| ReducerFilters / ReducerDependencies / ReducerGeneration | a reducer with an event filter, constructor dependencies other than `ILogger<T>` (self-reducing models are exempt), or an event type at two generations |
| Join / Hierarchy | joins, children or nested projections |
| OpenEndedEventTypes / Derivatives / FromEventProperty | all-events, FromEvery, or event-property routing |
| NotEventSourceKeyed / KeyConversion | keys not convertible to the admitted raw source key |
| NoEventTypes / UnsupportedEventTypeId | no finite guarded type set or unsupported encoded type ids |
| InvalidKey / FoldIncomplete / DefinitionMissing / DefinitionMismatch | runtime key, fold, missing kernel definition or client/kernel definition disagreement |
| StreamKeyRequiresStreamScope | stream-keyed model read without a stream scope |
| StreamScopeNotSupported / AllStreamsNotSupported / StreamKeyRequiresExplicitStreamId | unsupported stream reader or incomplete stream selection |

Whole-source key admission supports string/Guid schemas, with canonical lowercase
D-form Guid source ids assumed. A renamed or custom-converted key is not made
safe by a cast in the caller. Runtime definition comparison is a diagnostic, not
proof of equality with an executing projection. Keep integration specifications
for the actual kernel pair and model shape.

Do not silently replace a refused decision read with a materialized sink read.
Redesign the decision model as a focused admitted projection only when the
contract permits it, or surface the missing capability.

Requires Chronicle 19.75.0 for `DefinitionMissing` to be distinct from
`DefinitionMismatch`. Refusals are not cached, a read checks definitions once
more before refusing, and agreement is invalidated on connect/disconnect. A
matching definition can therefore recover after restart without restarting the
application. Check `dotnet list package`; older clients can retain a refusal.

## Completion is owned

`IUnitOfWork.HasEnrolledDecisionReads` is sticky: it remains true after commit,
rollback or disposal. It does not mean the transaction is still open.
`UnitOfWork.ClaimDecisionReadCommitOwnership()` returns the opaque
`DecisionReadCommitOwner` used by `CommitAsOwner` and `RollbackAsOwner`; these are concrete ownership capabilities, not methods on
`IUnitOfWork`. Once ownership is claimed, nested work must not complete that
protected transaction using ordinary commit/rollback. Arc's ownership wrapper
belongs in **cratis-arc-command** / **cratis-arc-command-validation**, not custom
application plumbing.

`ChronicleOptions.UnitOfWorkLifecyclePolicy` defaults to `Compatibility` for major
19; `Strict` opts in and is planned as the next-major default. Compatibility
retains legacy late staging only for unprotected work and logs it. Strict rejects
all late staging. Protected work and owner completion checks remain enforced;
`UnitOfWorkIsCompleting` must not be caught and interpreted as success. An
in-flight commit cannot be undone by a competing rollback.

Requires Chronicle 19.76.0 for `IUnitOfWork.SuspendReadRegions()`: nested hosts
can suspend inherited regions and restore them by disposing the scope. This is
not permission to bypass a guarded decision. Arc uses it from 22.113.0;
read-model injection outside the command decision under `On` is refused unless
the model is deliberately `[Unprotected("why")]`.

Requires Chronicle 19.81.0 for `RefuseImmediateAppends()`, which changes immediate
write admission, not read admission. Read
**cratis-chronicle-client-dotnet**'s immediate-append refusal reference before
claiming completion rights for its returned frame.

Keep read, decision, enrollment and completion inside one ambient async context.
If a commit rejects the read, start a new read/decision attempt rather than
reusing the stale protected instance or appending its events without the guard.
