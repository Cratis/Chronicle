---
name: cratis-chronicle-decision-reads
description: Guard a state-dependent Chronicle decision with IDecisionReads, projection admission, ambient unit-of-work enrollment and commit conflict handling. Use when an append must remain valid for the facts read, when choosing whole-source versus stream-scoped reads, or diagnosing DecisionReadRefused and scope conflicts. Do not use for ordinary query reads, append-time uniqueness alone, or defining an Arc command's ProtectedDecision API.
license: MIT
---
<!-- cratis-ai-managed: skills/cratis-chronicle-decision-reads/SKILL.md -->

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Guard the facts a decision used

A fresh read is not enough: another writer can change its facts between reading
and appending. A decision read folds an admitted projection from events and
carries a guard to commit. The append is rejected when a later event inside that
guard could change the decision. This is the Chronicle side of a dynamic
consistency boundary (DCB), not authorization or a replacement for unique constraints.

## Verified product sources

| Product | Version | Sources |
| --- | --- | --- |
| `Cratis.Chronicle` | `19.50.1` | `Source/Clients/DotNET/ReadModels/{IDecisionReads,DecisionReads,DecisionRead,DecisionReadRefusalReason}.cs` |
| `Cratis.Chronicle` transactions | `19.50.1` | `Source/Clients/DotNET/Transactions/{IUnitOfWork,UnitOfWork,DecisionReadCommitOwner,UnitOfWorkLifecyclePolicy}.cs` |
| `Cratis.Chronicle` scope resolution | `19.50.1` | `Source/Clients/DotNET/EventSequences/Concurrency/DefinitionConcurrencyScopeResolver.cs` |

The client surface began in 19.9.0; stream-scoped reads require the newer kernel
surface added in 19.39.0. Reverify a client/kernel pair rather than disabling
compatibility checking to force a read through.

## Route the work

- For Arc `[ProtectedDecision]`, Arc command/handler shapes and Arc `IStreamReads`,
  use **cratis-arc-command** and **cratis-arc-command-validation**; do not recreate
  their unit-of-work ownership in a handler. The Chronicle `DecisionRead<T>`
  below is not a license to invent an Arc wrapper shape.
- For a unique value claimed atomically or a closed stream, use
  **cratis-chronicle-event-constraints**.
- For read-model declarations, use **cratis-chronicle-read-model** and
  **cratis-chronicle-projection**. Ordinary `IReadModels` reads do not enroll guards.
- In model-first scope, settle the rule and its refusal/concurrency specifications
  in the accepted model using **cratis-screenplay-event-modeling** before gap-fill.
  Never broaden a projection or bypass model protection just to make admission pass.

## Check admission, then read

`IDecisionReads` is in `Cratis.Chronicle.ReadModels`; obtain the reader with
`eventStore.GetDecisionReads()` (`ReadModels/DecisionReadExtensions.cs`).
`Admit<T>()` / `AdmitForStream<T>()` check shape **without I/O**, returning
`DecisionReadAdmission(IsAdmitted, Reason)`. A successful shape check is not
runtime read success: definitions, key conversion and fold completion are also
checked while reading.

`Get<T>(ReadModelKey key, cancellationToken)` requires an ambient unit of work
and enrolls the protected read into it. Without one it throws
`DecisionReadRequiresUnitOfWork`. `GetDetached<T>` reads without enrollment:
explicitly add it with `IUnitOfWork.AddDecisionRead(read)` or carry it to a guarded
append; dropping its guard makes the decision unprotected.

`DecisionRead<T>` exposes `Instance` (nullable), `Exists`, `Key`, `IsProtected`,
and store/namespace/sequence/stream routing. An absent instance is still a fact
to guard, not permission to return a fabricated default model. Decide the
business rejection or creation behavior explicitly.

Read [admission and ownership](references/admission-and-ownership.md) when a
shape is refused, a read crosses a stream boundary, or nested work tries to
complete a protected transaction.

## Read one stream instead of the whole source

The stream overload takes `eventSourceId`, `eventStreamType`, `eventStreamId`,
optional `eventSourceType`, and cancellation; it has both `Get<T>` and
`GetDetached<T>` forms. It folds only that stream and guards it independently of
the model's key. It still admits **projections only**, not reducers or arbitrary
queries. Select one stream type, not `EventStreamType.All`; a stream-keyed
projection also needs an explicit stream id, not the default wildcard.
`AdmitForStream<T>()` checks this shape; unsupported readers/kernels refuse with
`StreamScopeNotSupported`. Do not fall back to an unguarded query on refusal.

## Commit and inspect conflicts

Enroll the read **before** staging its decision's events through
`EventLog.Transactional` and let the owning unit of work commit. Inspect
`IsSuccess`, append errors/constraint violations/concurrency violations and
`GetDecisionConflicts()`; the latter reports decision conflicts without exposing
sequence numbers. A checked empty append validates its scope without writing
an event. Do not mistake "no event" for "no consistency check".

Do not attach a separate explicit concurrency scope to a source already guarded
by a decision read: `DecisionReadConflictsWithConcurrencyScope` rejects that
combination. A batch needing incompatible scopes for one event source throws
`IncompatibleConcurrencyScopesForEventSource`. These are configuration/enrollment
errors, not an ordinary business rejection. Choose one coherent scope; do not
catch them and append without protection.

On a real conflict, reread and **re-decide** with the same authorization and
business rule; never resend stale events as if a retry fixed the decision. A
DCB guard protects event facts, not external side effects. Durable follow-up
belongs in a reactor/outbox; use **cratis-chronicle-reactor**.

## Verify

- The model is admitted and the read is protected and enrolled in the intended
  store, namespace and stream before staging events.
- Refusal is surfaced, never replaced by a materialized/unprotected read.
- One owner completes or rolls back; nested code does neither behind it.
- Competing relevant events cause rejection; unrelated events outside the guard
  do not. Absence, empty checked append and stream isolation have specifications.
- Scope conflicts, lifecycle violations and failed commit remain visible.
- Real-client/kernel integration proves the concurrency boundary; a default
  `EventScenario` scope strategy returning `None` is not concurrency evidence.
