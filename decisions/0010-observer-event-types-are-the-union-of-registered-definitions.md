---
id: 0010-observer-event-types-are-the-union-of-registered-definitions
title: An observer's event types are the union of its registered definitions, events route to a target that handles them, and an unhandled type parks its partition until a handler or an explicit retirement arrives
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-16
applies-to:
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs
  - Source/Kernel/Concepts/Observation/**
  - Source/Kernel/Storage/Observation/**
  - Source/Kernel/Storage/Keys/**
  - Source/Kernel/Storage.InMemory/Observation/**
  - Source/Kernel/Storage.MongoDB/Observation/**
  - Source/Kernel/Storage.MongoDB/Keys/**
  - Source/Kernel/Storage.Sql/EventStores/Observers/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/Observers/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/ObserverKeyIndexes/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/FailedPartitions/**
  - Source/Kernel/Contracts/Observation/**
  - Source/Clients/DotNET/Reactors/**
  - Source/Workbench/Features/EventStore/Namespaces/Observers/**
  - Source/Workbench/Features/Observation/**
  - Integration/Client/for_Observers/**
  - Integration/Clustering/for_ScaledOutClients/**
  - Documentation/concepts/observers.mdx
  - Documentation/reactors/**
  - Documentation/webhooks/**
---

<!-- markdownlint-disable-next-line MD025 -->
# An observer's event types are the union of its registered definitions, events route to a target that handles them, and an unhandled type parks its partition until a handler or an explicit retirement arrives

Proposed on 2026-10-16 for Cratis/Chronicle#4955 (events of a type silently skipped across an event-type change) and Cratis/Chronicle#4987 (clients with different event-type sets: last subscriber wins, and the observer stalls when that client disconnects). L0 approved the direction on Sindre Wilting's delegation on 2026-10-16. Two concurrency reviews (Astra) followed; the second left findings open that all came from readers running across a membership change. L0 then approved a structural simplification, recorded here: **a type-set change quiesces the observer**, so no reader ever runs across one. This revision is offered for acceptance by L0 and is not in force yet; what still needs L0 is under [Open questions for L0](#open-questions-for-l0), and [Review disposition](#review-disposition) maps every review finding to its outcome.

Paths are relative to `Source/Kernel/Core/Observation/` unless they start with `Source/`, `Integration/` or `Documentation/`. Line numbers are at `c46f21d943` (release 19.73.1).

## Context

### Who loses events

During a rolling deployment two versions of one application register the same observer id: version N handles `[A]`, version N+1 also handles `E` and registers `[A, E]`. Pods alternate as the rollout proceeds, and again on rollback. Every application with more than one instance is exposed, including Stage-rendered applications and Studio, which works around it by policy (Cratis/Studio decision 0020: changing a reactor's handled events "requires a separate reactor or a stop-the-world release"). Today events of `E` appended while `[A]` is current are skipped permanently, and while both versions are connected only one is served.

### Root cause

- **The last registration replaces the type set.** `SetUpSubscription` (`Observer.cs:907-935`) writes the registering definition's types over the stored definition and the subscription (`ApplySubscription`, `:937-996`). That definition is event-store-level storage written by every namespace's grain (`ObserverDefinitionGrainStorageProvider.cs`).
- **Different sets never coexist.** A second client becomes a fan-out target only with an identical set, filters, subscriber type and delivery mode (`CanFanOutInto`, `:998-1005`); otherwise it replaces the single target (`:983-995`). `Core.Specs/Observation/for_Observer/when_subscribing/and_another_client_instance_subscribes_with_different_event_types.cs` pins this.
- **The disconnect stall (#4987).** `UnsubscribeIfMatchesClient` (`Observer.cs:464-501`) unsubscribes when no target remains; the narrow client's target was dropped when the wide client registered, so the wide client's disconnect stops the observer although the narrow client is connected. The watchdog does the same (`Observer.Watchdog.cs:180-198`).
- **One type-agnostic position.** `NextEventSequenceNumber` advances by whatever is handled, routed, fast-forwarded or handed over (`Observer.Handling.cs:293-311`). Nothing records which types a position covers.

| Path that skips an event of a type outside the current set | Where |
| --- | --- |
| Live queue filters to the current types; the next handled event moves `Next` past it | `Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:217-235, 334-349`; `Observer.Handling.cs:297` |
| Live batch with no subscribed type moves `Next` to its tail | `Observer.Handling.cs:166-176` |
| A client without the handler acknowledges the event | `Source/Clients/DotNET/Reactors/ReactorInvoker.cs:148-152` |
| Routing, watchdog and `Observing` look only for current types and fast-forward | `States/Routing.cs:105-114, 156-161`; `Observer.Watchdog.cs:343-369`; `States/Observing.cs:69-82` |
| Leaving routing rewrites the definition's types | `States/Routing.cs:49-60` |
| Catch-up, partition catch-up, retry, replay and job steps read only the requested types; handover moves `Next` to the last scanned position | `Observer.Catchup.cs:159-191, 313-321`; `Observer.Failing.cs:325-341`; `Jobs/RetryFailedPartition.cs:111-160`; `Jobs/HandleEventsForPartition.cs:182-206`; `States/Replay.cs:62` |
| Key enumeration filters partitions by the shared definition's types | `Source/Kernel/Storage.MongoDB/Keys/ObserverKeyIndexes.cs:24-26`, `ObserverKeys.cs:40-43`; `Source/Kernel/Storage.Sql/EventStores/Namespaces/ObserverKeyIndexes/ObserverKeyIndexes.cs:27-28`; used by `Jobs/CatchUpObserver.cs:121-128`, `Jobs/ReplayObserver.cs:62-69` |

The kernel cannot notice the change either: `TransitionToReplayIfNeeded` (`Observer.Replay.cs:241-289`) evaluates after the definition was overwritten, so `HasDefinitionChanged` (`States/ReplayEvaluator.cs:59-61`) compares identical sets. Projections and reducers recover through `DefinitionEvolution` (`Source/Kernel/Core/Projections/Projection.cs:572-631`, `Reducers/Clients/Reducer.cs:151-183`).

### Why per-type recovery and in-flight cutover were rejected

The first design owed each re-added type its missed interval. A counterexample from the code broke it: a failed partition P at `E` 50 is retried with the narrow set (`Observer.Failing.cs:336`), handles `A` 70 and clears its whole failure (`:297-323`), so `E` 50 is never read; clamping the debt to 50 instead re-delivers `E` 60 on another partition. The first review found the model unsound in general (partial-success failure positions, catch-up partitions ahead of `Next`, repeated cycles, narrow clients acknowledging unhandled events). A correct version needs durable typed per-partition obligations, which decision 0004 (proposed) designs for a different engine. This record parks instead of recovering.

The second design kept readers running across a membership change and cut them over by revision checks. The second review showed that cutover is not provable against this code: the grain's in-memory tail is advanced after the storage commit (`Source/Kernel/Core/EventSequences/EventSequence.cs:206, 827-831`), so an old reader can pass an event the join position is computed below; old callbacks also clear catch-up markers and transition state, not just positions; resumed jobs keep their old type set (`JobsManagerExtensions.cs:124-152`). Quiescing removes the reader that crosses the change instead of proving it harmless.

## Decision

An observer's event-type set is the **union** of the types declared by the definitions registered for it. A narrower registration never shrinks it; a type leaves only through an explicit, audited **retire event type** operation, available with its CLI command and Workbench action in the same release. A change that grows or shrinks the set **quiesces** the observer first. Each event is routed to a connected target that declares its type. When none does, the event's partition **parks** until one arrives or the type is retired. A type joins at the first registration that declares it (decision B): earlier history is not delivered unless the observer is replayed. Narrowing is loud: a client warning, pending-retirement types in the Workbench and CLI, and visible parked partitions with a metric and an alert.

### 1. Scope

- **Adopted by** client reactors (`Reactions/Clients/IReactorObserverSubscriber.cs`), webhooks and event-store subscriptions. For webhooks and event-store subscriptions the single kernel target's capabilities are the declared subscription, so narrowing one leaves the removed type pending retirement.
- **Unchanged:** kernel reactors (`Reactors/Kernel/Reactors.cs:99-141`), captures and pattern captures (they already merge additively, `Source/Kernel/Core/Captures/CaptureEventsSubscriptions.cs:185`, `Source/Kernel/Core/Patterns/PatternCapture.cs:106`), projections and reducers. Kernel reactors ship with the kernel, so their set is the executing code's; a mixed-version kernel cluster keeps today's behavior for them. Projections are kernel-hosted, single-definition and served by `DefinitionEvolution`. Reducers are client-hosted but keep `DefinitionEvolution`, because union routing would let two fold versions write one read model; #4987 stays open for them, and the shared machinery is regression-specified to leave them unchanged.
- `SubscribesToAllEvents` (projections only, `Source/Kernel/Core/Projections/ProjectionsManager.cs:498`) is a wildcard scope (section 8) and has no membership.

### 2. Membership lives in one control record per namespace observer

- The **control record** lives in the per-namespace observer state, whose only writer is that namespace's observer grain. It holds: a monotonic **revision**; the **members**, each with its join revision (its *incarnation*), pins and join fence; the latest registration identity per connection (connection id, registration id, declared types); partition **continuations** (section 3); stored **retirement results**; and a pending change, if one is in progress.
- The event-store-level `ObserverDefinition.EventTypes` is written as the union for display (`ObserverInformation.cs:29-42`) and never read back as authoritative.
- Each change is one atomic write of the record: one JSON column in SQL, one `$set` of one sub-document in MongoDB (whose observer-state save is already a targeted `$set`, `Source/Kernel/Storage.MongoDB/Observation/ObserverStateStorage.cs:73-102`). `WriteStateAsync` overlays the in-memory record on every write, as it already does for alert fields (`Observer.cs:650-687`), so a transition returning an older state record cannot regress it.
- Every reader takes its type scope from the record: the queue subscription, the batch fast path, routing and watchdog lookups, the `Observing` missed-events check, every job request, `GetEventTypes`, and `IObserverKeyIndexes.GetFor`, which takes the job's explicit scope instead of looking the shared definition up.

### 3. A type-set change quiesces the observer

A registration that declares a type that is not a member, or a retirement (section 6), moves the namespace observer into a new **`ChangingDefinition`** running state. Nothing else does: a registration whose types are already members, a narrower registration, a reconnect and a disconnect only update target capabilities (section 4) and never quiesce. The quiesce affects only that namespace's observer; events appended meanwhile wait in the log.

1. **Intent.** The pending change (added types or retirement, with the registration identity) and the running state are written durably. The registration call returns; the .NET client does not wait for an acknowledgement (`Source/Clients/DotNET/Reactors/Reactors.cs:548-585`). Further registrations and retirements that arrive while changing merge into the same pending change; because union is commutative, concurrent registrations converge on one result whatever their order.
2. **Stop admitting work.** The queue subscription is removed (leaving `Observing`), and `Handle` already drops batches while the observer is not `Active` (`Observer.Handling.cs:394`). Catch-up, partition recovery, retry reminders, replay requests and watchdog fast-forward are refused while changing. Callbacks from running jobs are still accepted, because the current revision is still authoritative, but any state transition a callback would trigger (such as entering `Routing` after handover, `Observer.Catchup.cs:186-189`) is deferred to step 7.
3. **Drain.** Every unfinished job of the observer key is stopped: catch-up, partition catch-up, retry, hold-resume, replay and partition replay. Today's `PauseJobs` (`Observer.cs:1034-1048`) excludes replay; the quiesce does not. A stopped step checks cancellation between batches (`Jobs/HandleEventsForPartition.cs:173, 218`) and has persisted its last handled position after each batch (`:245`); its job reports stopped only once every step has (Cratis.Orleans `Job.Stop`/`HandleCompletion`, v1.16.3 `Source/Orleans/Jobs/Job.cs:279-302, 540-556`). An in-flight subscriber call is bounded by the observer's subscriber timeout. The observer polls job status on a grain timer, because the observer grain is not reentrant and cannot await its own jobs' callbacks inside one turn.
4. **Join position.** When drained, the observer reads the committed tail **from storage** (`IEventSequenceStorage.GetTailSequenceNumber`, `Source/Kernel/Storage/EventSequences/IEventSequenceStorage.cs:220`), not the grain's in-memory tail. Each added type's fence is `J = tail + 1`, or `EventSequenceNumber.First` when the sequence is empty (`Unavailable` survives addition, `Source/Kernel/Concepts/Events/EventSequenceNumber.cs:20, 97`).
5. **Harvest.** The persisted last handled position of every stopped non-replay step becomes that partition's continuation: for catch-up steps, a partition continuation; for retry and hold-resume steps, the failed partition's resume position. Replay steps are not harvested; replay restarts (section 5).
6. **Commit.** One atomic write publishes the next revision: members with their fences, registration identities, continuations, retirement results, and the pending change cleared. Only then are the old jobs removed. A job whose request carries an older revision is never adopted or resumed (`JobsManagerExtensions.cs:124-152` and `ResumeJobs`, `Observer.cs:1155-1181`, filter it out) and is removed when found.
7. **Resume through routing.** The queue is subscribed with the new set and revision; catch-up runs from `Next` and excludes partitions with continuations, which get partition catch-ups from their continuation (`Observer.Catchup.cs:313-321`); retries run from their failed partitions; holds whose awaited type is now declared or retired resume; an interrupted replay restarts.

**Why no reader crosses a change.** Every reader of revision *r* stopped before `J` was read, so every position it scanned or passed held an event already committed, and therefore lies below `J`. Whatever it skipped of an added type is history below the join, which decision B excludes anyway. Every reader after the commit runs with revision *r + 1*, the full set and the fences. Reading the tail from storage, not the in-memory tail, is what makes this hold: an event committed but not yet reflected in the grain's tail is still below `J`, so an old reader that passed it never passed an event at or above `J`. Appends are numbered and committed serially by the event sequence grain (`EventSequence.cs:793-831`), so an append still in flight when the tail is read gets a number at or above `J` and is delivered. The storage read is the linearization point; no append barrier is needed.

**Stale results are discarded.** Job requests, job callbacks and live queue batches carry the revision they were created under. After a commit, a callback or batch from an older revision is ignored **entirely**: it advances no position, clears no marker and triggers no transition. Before a commit, old-revision callbacks are current and settle normally.

**Bounded, observable, recoverable.** The pause lasts as long as the job stops take, bounded per in-flight batch by the subscriber timeout. The running state, the pending change, its age and the jobs still draining appear in the Workbench and the CLI; a gauge of changing observers, a duration histogram and entry and exit logs carrying the revision are emitted. If draining exceeds `Observers.DefinitionChangeTimeout`, the watchdog re-issues the stops on every tick and raises a warning alert. An authorized operator command, **complete definition change** with `--force`, removes the remaining jobs without harvesting them and commits; their unharvested progress is delivered again, and a step that ignores cancellation could still deliver out of order until it stops, so this is the one operator-only path that weakens per-partition order. A crash before the commit re-enters `ChangingDefinition` from the durable intent, with stopped steps' positions still in job-step storage; a crash after it removes old-revision jobs on sight. No path needs a manual storage edit.

### 4. Routing to targets by their declared types

- `ObserverSubscriberTarget` (`ObserverSubscriberTarget.cs:13`) gains its registration's declared types, generation pins and `EventGenerationDelivery` mode. A registration from a known connection replaces that target's capabilities; a new connection adds a target. Targets with different sets coexist.
- The selector chooses among targets whose capabilities cover the event, with the configured strategy over that filtered list (`RoundRobinObserverSubscriberSelector.cs:26-32`, `RandomObserverSubscriberSelector.cs:25-31`). Generation release happens per run with the selected target's mode and pins, not one observer-level mode (`Observer.Handling.cs:371-377`, `Jobs/HandleEventsForPartition.cs:443-458`), honoring decision 0002, item 3.
- A partition's batch is delivered as consecutive runs in sequence order, each the longest prefix one capable target covers, in live delivery and in every job step (which today selects one target per step, `Jobs/HandleEventsForPartition.cs:205-206`). The run before an event no connected target covers is delivered, and the partition parks at that event, so a later event of the partition is never delivered before an earlier one.
- Subscriber type and filters stay observer-level. A registration that differs in either is rejected with `IncompatibleObserverRegistration` and existing targets stay; newest-wins filters would silently discard events in the queue (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:334-344`).
- The .NET client fails a delivery it has no handler for with `NoHandlerForDeliveredEventType` instead of acknowledging it (`ReactorInvoker.cs:148-153`), except a type declared only for replay and delivered outside replay (`:306-323`). Older clients still acknowledge, so routing never sends a target a type it did not declare.
- Removing a target (`Observer.cs:464-501`, `Observer.Handling.cs:258-266`, `Observer.Watchdog.cs:180-198`) keeps the subscription while any target remains. This fixes #4987's stall for reactors and webhooks.

### 5. Parking, joining and replay

- **The hold.** A parked partition is a `FailedPartition` carrying a hold: episode id, reason `WaitingForHandler`, awaited type, exact resume position (the first undelivered event), and mode (live, or replay with its boundary). Reusing the entry holds the partition everywhere a failure already does (`Observer.Handling.cs:388`, `Jobs/CatchUpObserver.cs:128`). A hold registers no retry reminder (`Observer.Failing.cs:54-58`), adds no attempt, is excluded from both quarantine thresholds (`:355-383`) and reports `FailureKind.WaitingForHandler`. The global `Next` advances past it as past a failed partition; only the hold's owner moves the partition past its resume position.
- **Resuming** happens when a target declaring the awaited type subscribes (after the commit, if that registration added the type) or the type is retired. It is a retry job from the hold's resume position, in the hold's mode, carrying its episode; today's retry starts from `LastAttempt`, which is `NoAttempt` for a hold (`Source/Kernel/Concepts/Observation/FailedPartition.cs:65`), and always in live mode (`Jobs/RetryFailedPartition.cs:111-127`). The entry is cleared only by the existing recovered contract (read to the tail, then hand over, `Observer.Failing.cs:297-323`) with a matching episode. [Appendix A.2](#a2-hold-lifecycle) gives every transition.
- **A failed partition that also lacks a handler** delivers the run before the uncovered event (`Observer.Failing.cs:120-137`) and becomes a hold there, keeping its attempts.
- **Fences.** No non-replay reader delivers an event of a member type below that member's fence; fences are applied before handler selection. Lookups that ignore fences, such as fast-forward probes, only err toward not skipping. A brand-new observer has no fences: it catches up from the start, or from the tail when it starts at the tail (`Observer.StartAtTail.cs:15-35`).
- **Replay** reads every member and ignores fences. A membership change cancels a running replay and restarts it with the new set, so no replay spans a revision; a successful replay (`ReplayedSuccessfullySince`) therefore clears every fence, and a failed one (`Replayed`, `Jobs/ReplayObserver.cs:294-305`) clears none. A partition replay clears no fence. An event no target covers during replay gets a replay-mode hold that resumes as replay: `[OnceOnly]` and replay-only handlers apply and fences do not.
- **Operator skips** (fast-forward, clearing failed partitions; `Observer.FastForward.cs:44-47`, `Observer.Failing.cs:214-225`) discard holds too.
- **Visibility.** One warning alert per observer, namespace and awaited type, cleared only when no matching hold remains; parked-partition count and oldest-wait-age metrics (`ObserverMetrics.cs`).

### 6. Retirement

- **The operation** "retire event type from observer" is distinct from retiring a whole observer (`IObserver.Retire`, `Observer.Retirement.cs`). It runs in the namespace observer grain, serialized with registrations, and is keyed by an **operation id**:
  - A stored result for the id is returned as is; nothing runs again.
  - The type is not a member: `NotMember`, stored.
  - A connected target declares it: `Refused` with those connections, stored.
  - Otherwise the retirement becomes the pending change and the observer quiesces. The commit re-checks the declaration, because a registration may have arrived meanwhile, and stores `Retired` with the incarnation it removed, or `Refused`. The call returns `Pending`, and the result is queryable by operation id.
- **Results are never evicted.** They grow only with operator actions. A repeated id never acts again, so it cannot touch a later incarnation of the same type.
- **All namespaces.** An orchestration grain keyed by event store and operation id persists its arguments, the namespace snapshot taken when it first runs, each namespace's result, and whether its audit event was appended. Calling it again continues from that state and never re-enumerates namespaces. It appends one audit-only system event with the results, as webhook changes are recorded (`Webhooks/WebhookRegistrar.cs:62, 273`), using the operation id as its idempotency key (decision 0007); before keyed appends ship, a crash between append and record can append a second, identical audit event. No reactor applies that event, so replaying it changes nothing. A namespace created later is unaffected. The command is an `[Authorize]` operator command, as `Source/Kernel/Core/Sequences/ReopenStreamScope.cs:26`.
- **Effect.** The type leaves the members and its fence is dropped. Holds waiting only for it resume, skipping that type's events in those partitions; that skip is the decision being recorded. A later declaration joins as a new incarnation with a new fence.
- **Pending retirement** is the set of members no connected target declares. The kernel logs a warning at every narrower registration. The .NET client sends a registration id (section 9), queries that registration's pending types with a bounded wait, and logs a `WARNING` naming the observer, the types and `cratis chronicle observers retire-type <observer> <event-type>`.

### 7. Applying a registration is separate from deciding on replay

Today the client reactor grain unsubscribes the whole observer on any change (`Reactors/Clients/Reactor.cs:60-66`), and a webhook notifies its subscribers only when its comparer reports `Different` (`Webhooks/Webhook.cs:44-48`, `WebhookDefinitionComparer.cs:44-54`). Instead every registration and configuration change is applied: target capabilities are refreshed, webhook configuration is installed and subscribers are notified, without unsubscribing the observer or any other connection. Replay is considered only when a member is added: with `Observers.ReplayOnDefinitionChange` (`Source/Kernel/Core/Configuration/Observers.cs:92`) the resume plan runs one full replay, so alternating versions never replay twice; without it a recommendation is recorded. Narrowing never replays. This replaces the dead comparison in `TransitionToReplayIfNeeded`.

### 8. Explicit empty membership is not a wildcard

An empty type list means "every type" in the step fallback (`Jobs/HandleEventsForPartition.cs:182-185, 465-469`), the retry's events-left check (`Jobs/RetryFailedPartition.cs:147-153`) and MongoDB key enumeration (`Source/Kernel/Storage.MongoDB/Keys/ObserverKeys.cs:30-43`). Job requests and key-index lookups therefore carry an explicit scope, `Wildcard` or `Explicit(types)`, where `Explicit` may be empty. An empty explicit scope never starts a reader: retiring the last type settles its holds without a job, and routing moves a memberless observer to `Disconnected` (`States/Routing.cs:142-147`). A job persisted before the upgrade with an empty list is `Wildcard` only for an observer subscribed to all events; any other is removed without reading.

### 9. Release, kernel rollout and wire

- **One minor release, which is the downgrade floor.** The release note states it, as for other storage migrations. There is no bridge release and no activation marker.
- **Mixed-version kernel clusters.** A silo of this release reads from the Orleans cluster manifest whether every active silo carries a grain type introduced by this release; Chronicle has no silo-capability mechanism of its own (`Source/Kernel/Contracts/Clients/KernelCapabilities.cs` is client-facing). Until all do, every silo keeps today's behavior for type changes: the registration replaces the set, no control record, hold or fence is written, and retire-type is refused with `ClusterUpgradeInProgress`. Once all do, the next registration of each namespace observer initializes its control record from the stored definition (revision 1, members are the stored types, no fences) and then applies normally. The check gates only that initialization and is not re-evaluated.
  - **Guaranteed during the rollout:** nothing worse than today; no new-format state is written, so older silos never read it; once every silo runs this release, the full guarantees of this record apply from each observer's initialization.
  - **Not guaranteed during the rollout:** no loss across a type change, the #4987 fix, and recovery of events skipped while it lasts; those losses stay undetectable, as in decision 0004 Q7. An older silo joining after initialization is a downgrade below the floor and is unsupported.
- **Rolling client versions** are fully covered in any order once the cluster is upgraded.
- **Wire.** `RegisterReactor` gains an optional `RegistrationId` (`Source/Kernel/Contracts/Observation/Reactions/RegisterReactor.cs`, member 5). Without it the kernel behaves as today and warns in its log only; a new client talking to an older kernel skips the warning query, which is capability-gated. New operations are Core `[Command]`/`[ReadModel]` artifacts in a new `WellKnownServices` constant, generated per the project rule: retire type and its result query, complete definition change, registration status with pending retirement, and the observer's members, fences and definition-change state. The hand-written `Observers` service (`NonDerivedGrpcServices`, `Source/Kernel/Core/Core.csproj:45`) is not expanded. `FailureKind.WaitingForHandler`, the hold fields and `ObserverRunningState.ChangingDefinition` are appended with new member numbers only.
- **CLI** (Cratis/cli, `Source/Cli/Commands/Chronicle/Observers/`, tracked in a follow-up issue there): `retire-type` for one or all namespaces (polling the result), `complete-definition-change`, `show` with members, pending retirement and definition-change state, hold reasons in partition listings, and the terminal Workbench's `ObserversView`. It is released first, and the Chronicle release note names its minimum version.
- **Workbench:** observer details (`Source/Workbench/Features/EventStore/Namespaces/Observers/ObserverDetails.tsx`, `ObserverEventTypes.tsx`) and failed-partition views show members, pending retirement, holds and the definition-change state, and offer retire and complete definition change.
- **Documentation:** `Documentation/concepts/observers.mdx` states *an event type added to an existing observer is delivered from the first subscription that includes it; history is not replayed. To process history, replay the observer*, and covers the quiesce, holds, retirement and the downgrade floor. The reactors pages (`Documentation/reactors/replay.mdx`, `event-processing.mdx`) cover rolling deployments, the warning and filter compatibility; the webhook pages cover narrowing and retirement.

## Options considered

- **Per-type coverage with a debt catch-up job.** Unsound under failed partitions, partial success, handover, repeated cycles and acknowledging narrow clients. Rejected.
- **Typed recovery obligations with one ownership coordinator.** Correct, but a second progress authority inside the shared-cursor engine that decision 0004 replaces. Rejected for this engine.
- **Readers that run across the change with a revision cutover** (the second draft). Not provable against the in-memory tail and callback side effects. Rejected for the quiesce.
- **Parking only at recovery,** or **a union without per-target routing.** The set still shrinks in live paths, or a narrow client acknowledges what it cannot handle. Rejected.
- **Refusing a registration with a different type set.** Every handled-event change becomes a stop-the-world release. Rejected for types; kept for subscriber type and filters.
- **Automatic, time-based removal of types.** Cannot tell a finished rollout from a paused one or a scale-to-zero. Rejected.
- **Delivering history for a newly declared type (decision A).** Runs side effects for every old event. Rejected by L0.
- **A bridge minor with an activation marker.** Enforces the downgrade floor in software but still needs external admission for pre-bridge binaries, and adds a release. Rejected by L0 for a documented floor.
- **Including kernel reactors** with per-silo capabilities. Needs silo-incarnation target identity and liveness for code that ships with the kernel. Rejected by L0.

## Default if unanswered

Events of an added type keep being lost silently whenever versions alternate; clients with different sets keep replacing each other; the observer stalls when the winning client disconnects; changing a reactor's handled events keeps needing a separate reactor or a stop-the-world release. The loss is undetectable, and reactor and webhook effects cannot be rebuilt.

## Timeline and scope

Everything ships in **one minor release**. It never ships without retirement, its CLI command and Workbench action, because a narrowing would otherwise park partitions with no exit but fast-forward. Every new-behavior specification is red before its change.

1. **State and storage.** The control record, hold fields and `ChangingDefinition` in Concepts, Storage and all three providers; the SQL namespace migration in `Source/Kernel/Storage.Sql/EventStores/Namespaces/Observers/Migrations/`, named for the shipping release like `v19_15_0.cs` (failed partitions are `StateJson`); the MongoDB mapping; overlaying the record on every write. *Verify:* round trips on every provider; the migration on SQLite and PostgreSQL; a compatibility spec records whether the previous release's reader tolerates a document written by this release, which matters only below the floor.
2. **Kernel engine.** Quiesce, drain, harvest, commit and resume; revisions on jobs, callbacks and queue batches; authoritative scopes for readers and key indexes; per-target capabilities, run routing and incompatibility rejection; holds; fences; replay restart and fence clearing; explicit empty scopes; applying registrations apart from replay; the mixed-cluster gate. The pinning spec `and_another_client_instance_subscribes_with_different_event_types` is inverted. *Verify:* the Core.Specs and integration specs under [Verification](#verification), in `Integration/Client/for_Observers/`, `Integration/Clustering/for_ScaledOutClients/` and a mixed-silo scenario in `Integration/Clustering/`.
3. **Operations and visibility.** Namespace retirement and the all-namespace orchestration; the audit event; complete definition change; the derived service and queries; metrics, alerts and the kernel warning. *Verify:* Core.Specs and the wire-compatibility gates.
4. **Workbench.** The new state and both actions. *Verify:* Workbench specs and the project's Workbench verification workflow.
5. **CLI (Cratis/cli), released first.** *Verify:* `Integration/Chronicle/for_Observers/` against a kernel built from PR 3.
6. **.NET client and documentation.** `RegistrationId`, the warning query and log, the explicit missing-handler failure, the documentation in section 9. *Verify:* client specs; `bash Documentation/verify-markdown.sh`.

**Out of scope:** kernel reactors, captures and pattern captures; per-target filters or subscriber types; mixed-version reducers (#4987 for reducers, a follow-up issue); #4583; the ordered-lanes engine (decision 0004); recovering events lost before the upgrade; warnings in clients other than .NET beyond the kernel log and the generated contracts.

## Open questions for L0

1. **A membership change restarts a running replay.** The replayed prefix is delivered again in replay mode. Deferring the change until the replay ends would instead exclude events of the added type appended meanwhile. *Recommendation:* restart; replay is already a redelivery.
2. **Forced completion of a stuck definition change** may deliver unharvested progress again and, while a stuck step still runs, out of order. *Recommendation:* accept it as an operator-only command with a confirmation prompt, since the alternative is a manual storage edit.
3. **Webhooks and event-store subscriptions stay in scope.** They are kernel-hosted but user-defined, so a narrowed webhook parks until the type is retired. Treating them like kernel reactors would keep today's silent skip on a remove-and-re-add. *Recommendation:* keep them in.
4. **Filter or subscriber-type changes are rejected** and need a new observer id or a stop-the-world release. *Recommendation:* accept for this release; a stop-the-world change means verified quiescence of targets and persisted jobs.

## Review disposition

| Finding | Outcome |
| --- | --- |
| R1 #1 key enumeration, #2 kernel capabilities, #6 replay fences, #7 comparer plumbing, #8 per-target mode, #10 empty scope | Resolved in round 2 and kept: sections 2, 1, 5, 7, 4 and 8. Replay clearing is simpler, because no replay spans a revision |
| R1 #3 existing-reader cutover | Gone: readers drain before the change, stopped steps' positions are harvested, and old-revision jobs are never adopted (section 3) |
| R1 #4 atomic publication | Gone: membership, fences, identities and continuations are published in one write after draining; the pending intent is not membership |
| R1 #5 hold ownership | A hold resumes as a retry carrying its episode and is cleared only by the existing recovered contract with that episode ([Appendix A.2](#a2-hold-lifecycle)) |
| R1 #9 retirement durability | Results are never evicted; the orchestration persists snapshot, results and audit state; the audit append is keyed (section 6) |
| R1 #11 downgrade barrier | Replaced by L0's ruling: one minor as the documented floor, with today's behavior in mixed kernel clusters (section 9) |
| R2 N1 tail ordering | The join position is the storage tail read after draining; the argument no longer depends on the in-memory tail |
| R2 N2 callback side effects | An old-revision callback is ignored entirely after the commit; before it, its transitions are deferred |
| R2 N3 evictable operation ids | Never evicted |
| R2 N4 empty-sequence join | `J` is `First` for an empty sequence |
| R2 N5 kernel target identity | Kernel reactors are out of scope |
| R2 N6 registration correlation | Optional `RegistrationId` on `RegisterReactor` |

## Verification

**Done when:**

- across wide → narrow → wide and narrow → wide → narrow → wide registrations, with and without both versions connected, every event of a member type at or above its fence is delivered to a target that declares it, held, or skipped by an explicit retirement or operator skip;
- registration transitions add no duplicate delivery, outside a forced definition-change completion; at-least-once redelivery after a crash or timeout is unchanged, because subscriber effects precede the durable outcome (`Observer.Handling.cs:255, 331-357`);
- no reader of an older revision advances progress after a commit, and no registration shrinks membership;
- the observer stays subscribed while any target remains;
- a definition change that cannot drain is visible and completes through an operator command without editing storage.

**Verify by** these specifications, each red before its change:

- **Rolling upgrade, both orders:** no loss and no duplicate delivery, with the observer behind at the join and with both versions connected.
- **Never-handled type:** no history on any non-replay path, including after a crash mid-change.
- **Rollback:** after the wide version leaves, partitions reaching `E` park and are visible in the Workbench, CLI, metric and alert; retiring `E` releases them.
- **Intentional removal:** the narrower registration logs the client and kernel warnings naming the type and the exact command; retirement then removes it.
- **Wide-client disconnect:** the narrow client keeps serving `A` in other partitions; the observer stays subscribed.
- **Concurrent registrations converge:** several registrations and a retirement during one change produce one commit, whatever the order.
- **Quiesce is bounded and recoverable:** a subscriber that never answers is cut off by the subscriber timeout; a job that never stops raises the alert after `DefinitionChangeTimeout`; forced completion commits; a silo crash before and after the commit both converge.
- **Per-partition order:** `[A1, E2, A3]` in one partition with only an `[A]` target delivers `A1`, parks at `E2`, and never delivers `A3` before `E2`.
- **Original counterexample:** P parks at `E` 50 without delivering `A` 70; the wide target resumes it; `E` 60 on Q is not repeated.
- **Join position:** an `E` event committed to storage but not yet in the grain's tail when the change drains lies below `J`; an append in flight during the tail read is delivered; an empty sequence fences at `First`.
- **Stale results:** an old catch-up callback, retry completion or queue batch arriving after the commit changes nothing; a stopped old-revision job is never resumed.
- **Harvest:** a catch-up stopped mid-partition continues from its last handled event without repeating it; the same for a retry.
- **Holds:** resume from the exact position without attempts; a replay hold resumes in replay mode and honors `[OnceOnly]`; holds never count toward quarantine; global `Next` advances past them.
- **Replay:** a change during replay restarts it with the new set; success clears fences, failure clears none.
- **Key indexes:** on MongoDB and SQL, two namespaces with different memberships each enumerate their own `E`-only partitions.
- **Routing and compatibility:** per-target pins and modes; a registration with different filters or subscriber type is rejected and existing targets stay.
- **Webhooks:** narrowing applies URL and authorization changes and notifies subscribers; a pending-retirement type parks.
- **Retirement:** refused while declared; idempotent per operation id, also after a re-declaration and 100 later operations; partial all-namespace results; a resumed orchestration does not re-enumerate; a later namespace is unaffected; replaying the audit event changes nothing.
- **Empty scope:** retiring the last type starts no reader on any provider; an old persisted job with an empty list does not read everything.
- **Mixed kernel cluster:** with an older silo present, behavior matches today and no new state is written; after it leaves, observers initialize on their next registration.
- **Unchanged:** reducers, projections, kernel reactors and captures behave as before.
- **Storage and wire:** round trips on InMemory, MongoDB and SQL; the wire-compatibility gates.

## Consequences

- **Easier:** rolling client deployments and rollbacks change a reactor's handled events without loss or a stop-the-world release, and Studio can later supersede the reactor clause of its decision 0020; two client versions share one observer; a disconnect no longer stalls reactors or webhooks; a missing handler shows as a waiting partition, not a silent skip.
- **Harder:** a change that adds or retires a type pauses that namespace's observer while it drains; removing a handled type is a deliberate operator step, and partitions receiving it wait visibly until then, which the release note calls out; jobs, callbacks and queue batches carry revisions; observer state carries a control record and holds; the release is a downgrade floor; filter or subscriber-type changes need a new observer id or a stop-the-world release.
- **Ruled out:** automatic removal of event types; history for newly declared types outside replay; per-type progress in the shared-cursor engine; readers that span a membership change.

## Appendix A: mechanics

### A.1 Definition-change state

| From | Trigger | To |
| --- | --- | --- |
| any running state but `Quarantined` | registration adding a type, or a retirement that passes its checks | `ChangingDefinition`, intent written |
| `ChangingDefinition` | another registration or retirement | `ChangingDefinition`, merged intent written |
| `ChangingDefinition` | every job of the key stopped or finished | tail read, harvest, single commit, old jobs removed, `Routing` |
| `ChangingDefinition` | `DefinitionChangeTimeout` exceeded | stops re-issued on each watchdog tick, warning alert |
| `ChangingDefinition` | forced completion | remaining jobs removed unharvested, commit, `Routing` |
| `ChangingDefinition` | silo restart | re-entered from the durable intent |

A quarantined observer records the intent and quiesces when it leaves quarantine, because quarantine already stops its work.

### A.2 Hold lifecycle

| From | Event | To |
| --- | --- | --- |
| delivery or retry | no connected target declares the event at `p` | waiting (episode `e`, resume `p`, mode) |
| waiting | a declaring target subscribes, or the type is retired | resuming: a retry job carrying `e` from `p` in the hold's mode |
| resuming | the job reads to the tail and hands over with `e` | cleared |
| resuming | a run delivered up to `p'` before an uncovered event | waiting (new episode, resume `p'`) |
| resuming | handler failure | ordinary failure with its attempts and retry budget |
| resuming | stopped by a quiesce | resume position harvested; resumes after the commit |
| resuming | restart | the surviving job is adopted only if it carries `e` and the current revision; otherwise removed and resumed from the durable position |
| any | fast-forward, clearing failed partitions, retiring the last type | cleared and recorded |

### A.3 Storage

- **Control record:** MongoDB `typeSetControl` sub-document written by its own `$set`; SQL JSON column added by the namespace observer migration; InMemory field. Holds travel inside the failed-partition entry (SQL `StateJson`, the MongoDB class map, InMemory).
- **Retirement orchestration:** grain state per event store and operation id, in the existing event-store-level storage.
- **Not verified in code:** whether the previous release's MongoDB observer-state reader ignores unknown elements. It matters only for a downgrade below the floor, and PR 1's compatibility spec states the outcome.
