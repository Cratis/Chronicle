---
name: cratis-chronicle-decision-reads
description: Choose which mechanism makes a write-side rule hold (constraint, Arc read protection with read-model parameters and validators, per-stream read model, stream decision, aggregate, explicit scope, reactor repair or an advisory read), and guard a Chronicle decision directly with IDecisionReads, projection admission, ambient unit-of-work enrollment and commit conflict handling. Use when an append must remain valid for the facts read, when a rule must hold under concurrent writers, when choosing whole-source versus stream-scoped reads, or diagnosing ReadChanged, DecisionReadRefused and scope conflicts. Do not use for ordinary query reads or append-time uniqueness alone; Arc command shapes belong to cratis-arc-command.
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

Read [choosing how a rule holds](references/choosing-how-a-rule-holds.md) first
when the question is which mechanism to use, or before writing code that reads
state and then appends. It carries the routing by rule shape, the matrix with what
not to use, the refusals, rollout with Measure, testing and the traps.

In an Arc application new code sets read protection `On`
(`<CratisReadProtection>On</CratisReadProtection>`); `Off` is the default when
nothing is set. Under `On` the read model is a parameter of `Handle()` or of the
`CommandValidator<T>` constructor, and the read guards the append; under `Off` and
`Measure` a validator over a plain read is a hint that guards nothing. A plain read under
`Off`, `Result<,>` and `concurrency: true` are never the guard.
`[ProtectedDecision]` with `DecisionRead<T>` is the older per-command opt-in; it
still works and is not obsolete.

Requires Arc 22.106.0 (read protection) with Chronicle client and kernel 19.71.0.
Check `dotnet list package --include-transitive`; older Arc has only the
per-command `[ProtectedDecision]` path, and the choosing reference names where it
differs.

## Verified product sources

| Product | Sources |
| --- | --- |
| `Cratis.Chronicle` | `Source/Clients/DotNET/ReadModels/{IDecisionReads,DecisionReads,DecisionRead,DecisionReadRefusalReason}.cs` |
| `Cratis.Chronicle` transactions | `Source/Clients/DotNET/Transactions/{IUnitOfWork,UnitOfWork,DecisionReadCommitOwner,UnitOfWorkLifecyclePolicy}.cs` |
| `Cratis.Chronicle` scope resolution | `Source/Clients/DotNET/EventSequences/Concurrency/DefinitionConcurrencyScopeResolver.cs` |

Requires Chronicle 19.9.0 for decision reads, and a Chronicle 19.39.0 kernel for stream-scoped reads.

Reverify a client/kernel pair rather than disabling
compatibility checking to force a read through.

Requires Chronicle 19.62.0 for host enrollment contributors:
`IUnitOfWork.AddEnrollmentContributor(IUnitOfWorkEnrollmentContributor)` returns
a disposable removal handle. Contributors require scopes before staging through
`AddEvent`, `AddEvents`, named-tag and transactional append paths. Equal scopes
merge; a conflict throws `ConflictingEnrollmentScope` without staging that call.
Required scopes remain until completion even after handle disposal; contributed
units accept only one sequence. Reentrant mutation and failed `Enrolled`
notifications fail closed (`UnitOfWorkMutatedDuringEnrollment`,
`UnitOfWorkEnrollmentNotificationFailed`). This is a host integration extension,
not a command-side alternative to returned guarded events. Check the client
package version; older custom units throw
`UnitOfWorkEnrollmentContributorsNotSupported` when attached.

Requires Arc 22.100.0 or later with Chronicle client 19.65.2 or later for Arc's
generated-identifier guard on direct unit-of-work and transactional writes,
including nested commands. Check `dotnet list package --include-transitive`;
older Arc needs returned events or an explicit whole-source `BeforeFirst` scope
for direct writes. A conflict against an enrolled generated guard is reported as
Arc's `ConflictingConcurrencyScopes`, without identifier or scope values, rather
than Chronicle's `ConflictingEnrollmentScope`; unrelated enrollment conflicts keep
the Chronicle failure. Read
[generated-target freshness](../cratis-arc-command/references/fresh-identifiers.md)
for the covered APIs, scope lifetime, rollback and immediate-append boundary.

## Route the work

- For Arc read protection, `[LookUpBy]`, `[Unprotected]`, `[ProtectedDecision]`,
  command/handler shapes and Arc `IStreamReads`, use **cratis-arc-command** and
  **cratis-arc-command-validation**; do not recreate their unit-of-work ownership in
  a handler. The Chronicle `IDecisionReads` below is for code outside an Arc
  command (endpoints, services, `Provide()` for a computed key), not a license to
  invent an Arc wrapper shape.
- For a unique value claimed atomically or a closed stream, use
  **cratis-chronicle-event-constraints**. A decision read guards one key's
  history, not a value across keys: it is the wrong tool for uniqueness.
- For read-model declarations, use **cratis-chronicle-read-model** and
  **cratis-chronicle-projection**. Ordinary `IReadModels` reads do not enroll
  guards, materialized or passive.
- In model-first scope, settle the rule and its refusal/concurrency specifications
  in the accepted model using **cratis-screenplay-event-modeling** before gap-fill.
  Never broaden a projection or bypass model protection just to make admission pass.

## Check admission, then read

`IDecisionReads` is in `Cratis.Chronicle.ReadModels`; obtain the reader with
`eventStore.GetDecisionReads()` (`ReadModels/DecisionReadExtensions.cs`).
`Admit<T>()` / `AdmitForStream<T>()` check shape **without I/O**, returning
`DecisionReadAdmission(IsAdmitted, Reason)`. The model needs an `Id` property
(string, or a Guid in lowercase hyphenated form) and an event-source-keyed flat
projection, or an unfiltered reducer on the event log with no constructor
dependencies other than `ILogger<T>` (self-reducing models are exempt). Requires
Chronicle 19.68.0 for reducers. A filtered reducer refuses with `ReducerFilters`,
one with dependencies with `ReducerDependencies`, an event type at two generations
with `ReducerGeneration`, and a reducer in a stream-scoped read with
`StreamScopeNotSupported`. A missing `Id` or an unconvertible key refuses with `KeyConversion`. A successful shape check is not
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

In Arc, prefer a per-stream read model
(`[ReadModel, EventStream<TSource>("Transactions")]` on a command routed with
`[EventSource<TSource>("Transactions")]` and `[EventStreamId("{Month}")]`); use
the stream overloads of `IDecisionReads` in `Provide()` for another stream or
source. Requires Arc 22.81.0 for the stream overloads inside Arc commands to use
command protection, caching and enrollment; older Arc refuses them.

## Commit and inspect conflicts

Enroll the read **before** staging its decision's events through
`EventLog.Transactional` and let the owning unit of work commit. Inspect
`IsSuccess`, append errors/constraint violations/concurrency violations and
`GetDecisionConflicts()`. Requires Chronicle 19.76.0 for the nullable
`ConflictingEventTypeId` and `ConflictingEventSequenceNumber` on decision conflicts
and concurrency violations. They name the first event matching the failed guard;
older kernels leave both null. Conflict JSON contains the read-model identifier,
not its CLR type. Check with `dotnet list package` before asserting these fields. On a kernel whose append validators accept an empty batch
(a kernel that does so when a scope carries an actual boundary or expects no matching event), a checked empty
append validates its scope and writes nothing; a kernel that rejects it surfaces
`DecisionReadValidateOnlyNotSupported`, which fails closed. Do not mistake "no
event" for "no consistency check", and assert validate-only in an integration
specification before relying on it.

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

## When not to use a decision read

A guard detects every matching event after its boundary, whoever wrote it, but it
protects only the decision that carries it. A writer that decides on the same facts
without a guard can still append on stale state, so the rule holds only when every
deciding writer is guarded or the rule is a constraint.

- Uniqueness or create-once: a constraint. Two keys that must each be unique are
  not a read problem.
- A UX hint or a display: a plain read. A decision read costs about four calls per
  attempt and refolds the key's whole history every time.
- A hot key with a long history: shorten the history, or accept an advisory read
  and repair with a reactor.
- External state, revise/redact, other sequences: no guard covers them.
- The unit commits in the ASP.NET Core middleware: it commits after the response
  and reports no concurrency violation. Call
  `IUnitOfWorkCompletionFeature.CommitAsync()` before responding.
- Arc under `Off` (the default when nothing is set): a command with a read model
  is unguarded unless it carries `[ProtectedDecision]`. Set `On` for new code.

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
