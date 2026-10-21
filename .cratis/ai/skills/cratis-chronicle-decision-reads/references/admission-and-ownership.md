<!-- cratis-ai-managed: skills/cratis-chronicle-decision-reads/references/admission-and-ownership.md -->
<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Admission and commit ownership

Source: `Cratis/Chronicle`:
`Source/Clients/DotNET/ReadModels/DecisionReads.cs`, `DecisionReadRefusalReason.cs`,
`Source/Clients/DotNET/Transactions/UnitOfWork.cs`, `DecisionReadCommitOwner.cs`,
and `UnitOfWorkLifecyclePolicy.cs`.

## Why a shape is refused

Admission requires exactly one supported projection on the event log. It is not
an adapter for arbitrary read-model storage:

| Reason | Shape to inspect |
| --- | --- |
| Reducer / AmbiguousProjection / NotEventLog | reduced model, several projections, or another sequence |
| Join / Hierarchy | joins, children or nested projections |
| OpenEndedEventTypes / Derivatives / FromEventProperty | all-events, FromEvery, or event-property routing |
| NotEventSourceKeyed / KeyConversion | keys not convertible to the admitted raw source key |
| NoEventTypes / UnsupportedEventTypeId | no finite guarded type set or unsupported encoded type ids |
| InvalidKey / FoldIncomplete / DefinitionMismatch | runtime key, fold or client/kernel definition disagreement |
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

Keep read, decision, enrollment and completion inside one ambient async context.
If a commit rejects the read, start a new read/decision attempt rather than
reusing the stale protected instance or appending its events without the guard.
