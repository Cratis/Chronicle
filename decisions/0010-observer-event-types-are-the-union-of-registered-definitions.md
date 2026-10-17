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
  - Source/Kernel/Contracts/Clients/KernelCapabilities.cs
  - Source/Clients/DotNET/Reactors/**
  - Source/Workbench/Features/EventStore/Namespaces/Observers/**
  - Source/Workbench/Features/Observation/**
  - Integration/Client/for_Observers/**
  - Integration/Clustering/for_ScaledOutClients/**
  - Documentation/concepts/observers.mdx
  - Documentation/hosting/configuration/observers.md
  - Documentation/reactors/**
  - Documentation/webhooks/**
---

<!-- markdownlint-disable-next-line MD025 -->
# An observer's event types are the union of its registered definitions, events route to a target that handles them, and an unhandled type parks its partition until a handler or an explicit retirement arrives

Proposed for Cratis/Chronicle#4955 (events of a type silently skipped across an event-type change) and Cratis/Chronicle#4987 (clients with different event-type sets: last subscriber wins, and the observer stalls when that client disconnects). It settles membership, routing, parking and retirement. How a change to the set is applied without a reader crossing it, and when the cluster may start doing so, is [decision 0011](0011-a-type-set-change-quiesces-the-observer-through-orleans-drain-and-admission.md); the two are proposed together and neither is in force without the other. Open points are under [Open questions](#open-questions).

Paths are relative to `Source/Kernel/Core/Observation/` unless they start with `Source/`, `Integration/` or `Documentation/`. Chronicle line numbers are at `bec5709f8c` (release 19.77.2).

## Context

### Who loses events

During a rolling deployment two versions of one application register the same observer id: version N handles `[A]`, version N+1 also handles `E` and registers `[A, E]`. Pods alternate as the rollout proceeds, and again on rollback. Every application with more than one instance is exposed, including Stage-rendered applications and Studio, which works around it by policy (Cratis/Studio decision 0020: changing a reactor's handled events "requires a separate reactor or a stop-the-world release"). Today events of `E` appended while `[A]` is current are skipped permanently, and while both versions are connected only one is served.

### Root cause

- **The last registration replaces the type set.** `SetUpSubscription` (`Observer.cs:907-935`) writes the registering definition's types over the stored definition and the subscription (`ApplySubscription`, `:937-996`). That definition is event-store-level storage written by every namespace's grain (`ObserverDefinitionGrainStorageProvider.cs`).
- **Different sets never coexist.** A second client becomes a fan-out target only with an identical set, filters, subscriber type and delivery mode (`CanFanOutInto`, `:998-1005`); otherwise it replaces the single target (`:983-995`). `Source/Kernel/Core.Specs/Observation/for_Observer/when_subscribing/and_another_client_instance_subscribes_with_different_event_types.cs` pins this.
- **The disconnect stall (#4987).** `UnsubscribeIfMatchesClient` (`Observer.cs:464-501`) unsubscribes when no target remains; the narrow client's target was dropped when the wide client registered, so the wide client's disconnect stops the observer although the narrow client is connected. The watchdog does the same (`Observer.Watchdog.cs:170-212`).
- **One type-agnostic position.** `NextEventSequenceNumber` advances by whatever is handled, routed, fast-forwarded or handed over (`Observer.Handling.cs:293-311`). Nothing records which types a position covers.

| Path that skips an event of a type outside the current set | Where |
| --- | --- |
| Live queue filters to the current types; the next handled event moves `Next` past it | `Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:217-235, 334-349`; `Observer.Handling.cs:297` |
| Live batch with no subscribed type moves `Next` to its tail | `Observer.Handling.cs:166-176` |
| A client without the handler acknowledges the event | `Source/Clients/DotNET/Reactors/ReactorInvoker.cs:148-152` |
| Routing, watchdog and `Observing` look only for current types and fast-forward | `States/Routing.cs:105-114, 156-161`; `Observer.Watchdog.cs:343-369`; `States/Observing.cs:69-82` |
| Leaving routing rewrites the definition's types | `States/Routing.cs:49-60` |
| Catch-up, partition catch-up, retry, replay and job steps read only the requested types; handover moves `Next` to the last scanned position | `Observer.Catchup.cs:159-191, 313-321`; `Observer.Failing.cs:325-341`; `Jobs/RetryFailedPartition.cs:111-160`; `Jobs/HandleEventsForPartition.cs:176-200`; `States/Replay.cs:62` |
| Key enumeration filters partitions by the shared definition's types | `Source/Kernel/Storage.MongoDB/Keys/ObserverKeyIndexes.cs:24-26`, `ObserverKeys.cs:40-43`; `Source/Kernel/Storage.Sql/EventStores/Namespaces/ObserverKeyIndexes/ObserverKeyIndexes.cs:27-28`; used by `Jobs/CatchUpObserver.cs:121-128`, `Jobs/ReplayObserver.cs:62-69` |

The kernel cannot notice the change either: `TransitionToReplayIfNeeded` (`Observer.Replay.cs:241-289`) evaluates after the definition was overwritten, so `HasDefinitionChanged` (`States/ReplayEvaluator.cs:59-61`) compares identical sets. Projections and reducers recover through `DefinitionEvolution` (`Source/Kernel/Core/Projections/Projection.cs:572-631`, `Reducers/Clients/Reducer.cs:151-183`).

### Why per-type recovery was rejected

A design that owed each re-added type its missed interval fails on this code: a failed partition P at `E` 50 is retried with the narrow set (`Observer.Failing.cs:336`), handles `A` 70 and clears its whole failure (`:297-323`), so `E` 50 is never read; clamping the debt to 50 instead re-delivers `E` 60 on another partition. Partial-success failure positions, catch-up partitions ahead of `Next`, repeated cycles and narrow clients acknowledging unhandled events break it in general. A correct version needs durable typed per-partition obligations, which decision 0004 (proposed) designs for a different engine. This record parks instead of recovering.

## Decision

An observer's event-type set is the **union** of the types declared by the definitions registered for it. A narrower registration never shrinks it; a type leaves only through an explicit, audited **retire event type** operation, available with its CLI command and Workbench action in the same release. A change that grows or shrinks the set is applied by quiescing the observer ([decision 0011](0011-a-type-set-change-quiesces-the-observer-through-orleans-drain-and-admission.md)). Each event is routed to a connected target that declares its type. When none does, the event's partition **parks** until one arrives or the type is retired. A type joins at the change that its first declaring registration starts: its fence is the log's tail once the observer has drained, so earlier history, including events of the type appended while that change drains, is not delivered unless the observer is replayed. Narrowing is loud: a client warning, pending-retirement types in the Workbench and CLI, and visible parked partitions with a metric and an alert.

### 1. Scope

- **Adopted by** client reactors (`Reactions/Clients/IReactorObserverSubscriber.cs`), webhooks and event-store subscriptions. Webhooks and event-store subscriptions are kernel-hosted but user-defined, so they have the same membership problem; for them the single kernel target's capabilities are the declared subscription, and narrowing one leaves the removed type pending retirement. All three have side effects that are not idempotent: a reactor's and a webhook's are the application's, and an event-store subscription appends to the target store's inbox without an idempotency key (`EventStoreSubscriptions/EventStoreSubscriptionObserverSubscriber.cs:92-105`). Decision 0011, section 9, therefore classifies all three as side-effecting when it settles jobs inherited from the previous release.
- **Unchanged:** kernel reactors (`Reactors/Kernel/Reactors.cs:99-141`), captures and pattern captures (they already merge additively, `Source/Kernel/Core/Captures/CaptureEventsSubscriptions.cs:185`, `Source/Kernel/Core/Patterns/PatternCapture.cs:106`), projections and reducers. Kernel reactors ship with the kernel, so their set is the executing code's. Projections are kernel-hosted, single-definition and served by `DefinitionEvolution`. Reducers are client-hosted but keep `DefinitionEvolution`, because union routing would let two fold versions write one read model; #4987 stays open for them, and the shared machinery is regression-specified to leave them unchanged.
- `SubscribesToAllEvents` (projections only, `Source/Kernel/Core/Projections/ProjectionsManager.cs:498`) is a wildcard scope (section 8) and has no membership.
- **In force per namespace observer** once its control record is initialized, which happens only after decision 0011's cluster gate opens. Until then the observer behaves as today.

### 2. Membership lives in one control record per namespace observer

- The **control record** lives in the per-namespace observer state, whose only writer is that namespace's observer grain. It holds a monotonic **revision**; the **members**, each with its join revision (its *incarnation*), pins and join fence; the **compatibility contract** (section 4); how the record was bootstrapped (from the namespace baseline, as an idle observer, or by an operator; decision 0011, section 9); the latest registration identity per connection (connection id, registration id if sent, declared types); stored **retirement results**; the state of the first change's legacy settlement; and the pending change, if any. Continuations, restart plans, the job inventory and terminal rows are not in it; decision 0011 stores them as rows.
- The event-store-level `ObserverDefinition.EventTypes` is written as the union for display (`ObserverInformation.cs:29-42`) and never read back as authoritative, not even at initialization, which takes the namespace's own baseline (decision 0011, section 9). Every namespace writes its own union there, so with different memberships per namespace the last writer wins; the Workbench's observer details read the namespace's members from the control record instead.
- Each change is one atomic write of the record: one JSON column in SQL, one `$set` of one sub-document in MongoDB (whose observer-state save is already a targeted `$set`, `Source/Kernel/Storage.MongoDB/Observation/ObserverStateStorage.cs:73-102`). `WriteStateAsync` overlays the in-memory record on every write, as it already does for alert fields (`Observer.cs:650-687`), so a transition writing an older state cannot regress it.
- Every reader takes its type scope from the record: the queue subscription, the batch fast path, routing and watchdog lookups, the `Observing` missed-events check, every job request, `GetEventTypes`, and `IObserverKeyIndexes.GetFor`, which takes the job's explicit scope instead of looking the shared definition up.

### 3. What changes the set

- A registration that declares a type that is not a member, or a retirement (section 6), starts a change; decision 0011 applies it and commits revision *r + 1* with the new members and, for each added type, the fence `J`. Nothing else changes membership: a registration whose types are already members, a narrower registration, a reconnect and a disconnect only update target capabilities (section 4).
- Registrations and retirements that arrive while a change is pending merge into it in the grain's serialized order: added types accumulate, so additions converge whatever their order; a later registration from the same connection replaces that connection's earlier one; a retirement is decided at the commit against the declarations current then (section 6).
- The registration call returns once the pending change is durable; the .NET client does not wait for an acknowledgement (`Source/Clients/DotNET/Reactors/Reactors.cs:548-585`).

### 4. Routing to targets by their declared types

- `ObserverSubscriberTarget` (`ObserverSubscriberTarget.cs:13`) gains its registration's declared types, generation pins and `EventGenerationDelivery` mode. A registration from a known connection replaces that target's capabilities; a new connection adds a target. Targets with different sets coexist.
- The selector chooses among targets whose capabilities cover the event, with the configured strategy over that filtered list (`RoundRobinObserverSubscriberSelector.cs:26-32`, `RandomObserverSubscriberSelector.cs:25-31`). Generation release happens per run with the selected target's mode and pins, not one observer-level mode (`Observer.Handling.cs:371-377`, `Jobs/HandleEventsForPartition.cs:468-483`), honoring decision 0002, item 3.
- A partition's batch is delivered as consecutive runs in sequence order, each the longest prefix one capable target covers, in live delivery and in every job step (which today selects one target per step, `Jobs/HandleEventsForPartition.cs:199-200`). The run before an event no connected target covers is delivered, and the partition parks at that event, so a later event of the partition is never delivered before an earlier one.
- **The compatibility contract.** Subscriber type and filters (event source type, event stream type and filter tags, compared in canonical form) stay observer-level. Today they live only in the activation's subscription, which starts empty (`Observer.cs:77, 998-1005`), so after a restart nothing would stop a changed filter from becoming the new baseline while stored progress and jobs describe the old one. They are therefore persisted per namespace as the contract in the control record, set at initialization from the namespace baseline (decision 0011, section 9). A registration whose subscriber type or filters differ from the contract is rejected with `IncompatibleObserverRegistration`, and existing targets stay; newest-wins filters would silently discard events in the queue (`Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs:334-344`).
- **Checked before anything authoritative is written.** Today a client reactor stores its definition before reconciling the subscription (`Reactors/Clients/Reactor.cs:57-58`), and a webhook stores its definition before notifying its subscribers (`Webhooks/Webhook.cs:42-43`). Instead the reactor grain asks its namespace observer to check the contract before it stores anything, and a webhook or event-store subscription change is checked against the contract of every initialized namespace before its event-store-level definition is stored or any subscriber is notified. The contract never changes for an observer id in this release, so there is nothing to propagate and no crash window in which a namespace holds a contract its definition does not: a definition that passed the check equals every initialized contract, and a namespace that initializes later compares its baseline with the registration it initializes on. Changing a filter or subscriber type takes a **new observer id**.
- **Capabilities are fixed per connection.** A delivery is handed to the client stream looked up by reactor and connection when it is dispatched (`Reactors/Clients/ReactorMediator.cs:31, 55`), so a capability change on a live connection could reach a run selected under the old capabilities, and a client released before this decision acknowledges a type it lacks. A registration from a connected connection that declares the same types, pins and mode (the .NET client re-establishing its stream) is the same target. One that declares different capabilities is rejected with `ConnectionCapabilitiesChanged`, and the existing target stays. Capabilities change with a new connection: the old connection's disconnect removes its target, and a delivery still in flight to it answers `Disconnected` (`:61`), which fails the partition for a retry instead of acknowledging it. The .NET client never changes a reactor's capabilities on a live connection: its handlers' types are fixed in the process, and a reconnect takes a new connection id (`Source/Clients/Connections/ConnectionLifecycle.cs:155`).
- The .NET client fails a delivery it has no handler for with `NoHandlerForDeliveredEventType` instead of acknowledging it (`ReactorInvoker.cs:148-153`), except a type declared only for replay and delivered outside replay (`:306-323`). It does so only when the kernel advertises a new `KernelCapabilities` constant (`Source/Kernel/Contracts/Clients/KernelCapabilities.cs`), which the kernel advertises once the cluster gate is open; against an older kernel, or before the gate, an in-flight step can still reach a just-narrowed client, and failing there would fail partitions and raise alerts where today it acknowledges silently. Older clients still acknowledge, so routing never sends a target a type it did not declare.
- Removing a target (`Observer.cs:464-501`, `Observer.Handling.cs:258-266`, `Observer.Watchdog.cs:170-212`) keeps the subscription while any target remains. This fixes #4987's stall for reactors and webhooks.
- **The #4987 hand-over is superseded.** The narrow fix (Cratis/Chronicle#5013) remembers superseded registrations and, when the winning client leaves, registers the most recent still-connected one again, replacing the subscription's and the definition's types with its set; events of types only the departed client handled are skipped. Here no registration is superseded: every connection is a target, a disconnect removes only that target, and membership never shrinks, so there is nothing to hand over. Once a control record is initialized the hand-over path is not taken, its superseded-registration list gives way to the per-connection identities in the control record, and the events it would skip park instead. Before initialization the hand-over is part of today's behavior. `Documentation/hosting/configuration/observers.md`, which that fix documents, is updated to match.

### 5. Parking, joining and replay

- **The recovery cursor.** A failed-partition entry carries, for holds and failures alike, its exact resume position, its **mode** (live, or replay with its boundary) and its **owner**. An entry written before initialization has neither; decision 0011, section 9, reads it as live mode from its last attempt and gives it an owner at its first recovery job. Position and mode do not depend on why the partition waits: a hold that turns into a failure, a failure that turns into a hold, retries and quarantine all keep them. A retry runs in the entry's mode up to the boundary and in live mode past it; today's retry always runs in live mode (`EventObservationState.None`, `Jobs/RetryFailedPartition.cs:118-126`), which would drop a replay-mode event below its fence or hand it to the wrong handler.
- **The owner** is `(episode, job, execution, state, revision)`: the episode of the hold or failure; the recovery job's id and the execution number the observer assigns when it starts it, both carried in the job's request; the entry's recovery state (waiting, retrying, resuming); and the control record's revision. The observer never resumes a stopped recovery job; it starts a replacement with the next execution number, so a late call from an earlier execution no longer matches. Every mutation a recovery job asks for (clearing, re-parking, recording a failure or an attempt, moving the resume position) is applied only when the whole tuple matches and is otherwise ignored entirely; admission (decision 0011) refuses to start or resume a recovery job whose tuple does not match.
- **The hold.** A parked partition is a `FailedPartition` carrying a hold: reason `WaitingForHandler` and the awaited type, with the cursor above; its resume position is the first undelivered event. Reusing the entry holds the partition everywhere a failure already does (`Observer.Handling.cs:388`, `Jobs/CatchUpObserver.cs:128`). A hold registers no retry reminder (`Observer.Failing.cs:54-58`), adds no attempt, is excluded from both quarantine thresholds (`:355-383`) and reports `FailureKind.WaitingForHandler`. The global `Next` advances past it as past a failed partition; only the hold's owner moves the partition past its resume position.
- **Resuming** happens when a target declaring the awaited type subscribes (after the commit, if that registration added the type) or the type is retired. It is a retry job from the hold's resume position, in the hold's mode, carrying its owner; today's retry starts from `LastAttempt`, which is `NoAttempt` for a hold (`Source/Kernel/Concepts/Observation/FailedPartition.cs:65`). The entry is cleared only by the existing recovered contract (read to the tail, then hand over, `Observer.Failing.cs:297-323`) with a matching owner. [Appendix A.1](#a1-hold-lifecycle) gives every transition.
- **A failed partition that also lacks a handler** delivers the run before the uncovered event (`Observer.Failing.cs:120-137`) and becomes a hold there, keeping its attempts and mode. A resuming hold that meets a handler failure becomes a failure with its attempts and retry budget, keeping its mode.
- **Fences.** No non-replay reader delivers an event of a member type below that member's fence; fences are applied before handler selection. Lookups that ignore fences, such as fast-forward probes, only err toward not skipping. A brand-new observer has no fences: it catches up from the start, or from the tail when it starts at the tail (`Observer.StartAtTail.cs:15-35`).
- **Replay** reads every member and ignores fences. A membership change stops a running replay and restarts it with the new set (decision 0011), so no replay spans a revision; a successful replay (`ReplayedSuccessfullySince`) therefore clears every fence, and a failed one (`Replayed`, `Jobs/ReplayObserver.cs:294-305`) clears none. A partition replay clears no fence. An event no target covers during replay gets a replay-mode hold that resumes as replay: `[OnceOnly]` and replay-only handlers apply and fences do not.
- **Operator skips** (fast-forward, clearing failed partitions; `Observer.FastForward.cs:44-47`, `Observer.Failing.cs:214-225`) discard holds too.
- **Visibility.** One warning alert per observer, namespace and awaited type, cleared only when no matching hold remains; metrics `chronicle-observer-partitions-waiting-for-handler` (count) and `chronicle-observer-oldest-handler-wait` (age), beside the existing ones in `ObserverMetrics.cs`.

### 6. Retirement

- **The operation** "retire event type from observer" is distinct from retiring a whole observer (`IObserver.Retire`, `Observer.Retirement.cs`). It runs in the namespace observer grain, serialized with registrations, and is keyed by an **operation id**:
  - A stored result for the id is returned as is; nothing runs again.
  - The type is not a member: `NotMember`, stored.
  - A connected target declares it: `Refused` with those connections, stored.
  - Otherwise the retirement becomes the pending change. The commit re-checks the declaration, because a registration may have arrived meanwhile, and stores `Retired` with the incarnation it removed, or `Refused`. The call returns `Pending`, and the result is queryable by operation id.
- **Results are never evicted.** They grow only with operator actions. A repeated id never acts again, so it cannot touch a later incarnation of the same type.
- **All namespaces.** An orchestration grain keyed by event store and operation id persists its arguments, the namespace snapshot taken when it first runs, each namespace's result, and whether its audit event was appended. Calling it again continues from that state and never re-enumerates namespaces. It appends one audit-only system event with the results, as webhook changes are recorded (`Webhooks/WebhookRegistrar.cs:62, 273`), using the operation id as its idempotency key (decision 0007); before keyed appends ship, a crash between append and record can append a second, identical audit event. No reactor applies that event, so replaying it changes nothing. A namespace created later is unaffected. The command is an `[Authorize]` operator command, as `Source/Kernel/Core/Sequences/ReopenStreamScope.cs:26`.
- **Effect.** The type leaves the members and its fence is dropped. Holds waiting only for it resume, skipping that type's events in those partitions; that skip is the decision being recorded. A later declaration joins as a new incarnation with a new fence.
- **Pending retirement** is the set of members no connected target declares. The kernel logs a warning at every narrower registration. The .NET client sends a registration id (section 9), queries that registration's pending types with a bounded wait, and logs a `WARNING` naming the observer, the types and `cratis chronicle observers retire-type <observer> <event-type>`. The query answers `Superseded` when the connection has registered again since, and `Unknown` for an id the observer does not hold; neither reads as "no pending types", and the client logs that the check could not be made.

### 7. Applying a registration is separate from deciding on replay

Today the client reactor grain unsubscribes the whole observer on any change (`Reactors/Clients/Reactor.cs:60-66`), and a webhook notifies its subscribers only when its comparer reports `Different` (`Webhooks/Webhook.cs:44-48`, `Webhooks/WebhookDefinitionComparer.cs:44-54`). Instead every registration and configuration change that passes the compatibility checks of section 4 is applied: target capabilities are refreshed, webhook configuration is installed and subscribers are notified, without unsubscribing the observer or any other connection. Replay is considered only when a member is added: with `Observers.ReplayOnDefinitionChange` (`Source/Kernel/Core/Configuration/Observers.cs:92`) the resume runs one full replay, so alternating versions never replay twice; without it a recommendation is recorded. Narrowing never replays. This replaces the dead comparison in `TransitionToReplayIfNeeded`.

### 8. Explicit empty membership is not a wildcard

An empty type list means "every type" in the step fallback (`Jobs/HandleEventsForPartition.cs:176-179, 490-494`), the retry's events-left check (`Jobs/RetryFailedPartition.cs:147-153`) and MongoDB key enumeration (`Source/Kernel/Storage.MongoDB/Keys/ObserverKeys.cs:30-43`). Job requests and key-index lookups therefore carry an explicit scope, `Wildcard` or `Explicit(types)`, where `Explicit` may be empty. An empty explicit scope never starts a reader: retiring the last type settles its holds without a job, and routing moves a memberless observer to `Disconnected` (`States/Routing.cs:142-147`). Until the observer's control record is initialized every scope reads as today, an empty list included (slice 1). Once it is, a job persisted before the upgrade with an empty list is `Wildcard` only for an observer subscribed to all events; any other is removed without reading.

### 9. Wire, operations and documentation

- **Wire.** `RegisterReactor` gains an optional `RegistrationId` (`Source/Kernel/Contracts/Observation/Reactions/RegisterReactor.cs`, member 5). It only correlates warnings. Every registration joins the union and routing whether or not it carries one, so a client released before this decision (which cannot send it, `Source/Clients/DotNET/Reactors/Reactors.cs:557-579`) widens membership, is a target for the types it declares, and never narrows or bypasses an initialized control record; it gets the kernel's log warning only. A new client talking to an older kernel skips the warning query, which is capability-gated. New operations are Core `[Command]`/`[ReadModel]` artifacts in a new `WellKnownServices` constant, generated per the project rule: retire type and its result query, registration status with pending retirement, and the observer's members, fences and definition-change state (decision 0011 adds complete definition change, allow legacy redelivery, initialize observer and the settlement state). The hand-written `Observers` service (`NonDerivedGrpcServices`, `Source/Kernel/Core/Core.csproj:45`) is not expanded. `FailureKind.WaitingForHandler` and the hold fields are appended with new member numbers only.
- **CLI** (Cratis/cli, `Source/Cli/Commands/Chronicle/Observers/`, tracked in a follow-up issue there): `retire-type` for one or all namespaces (polling the result), `show` with members, pending retirement and definition-change state, hold reasons in partition listings, and the terminal Workbench's `ObserversView`.
- **Workbench:** observer details (`Source/Workbench/Features/EventStore/Namespaces/Observers/ObserverDetails.tsx`, `ObserverEventTypes.tsx`) and failed-partition views show members, pending retirement and holds, and offer retire.
- **Documentation:** `Documentation/concepts/observers.mdx` states *an event type added to an existing observer is delivered from the moment its first subscription takes effect, after the observer has paused to apply it; events of the type appended before that, including during the pause, are not delivered. To process history, replay the observer*, and covers holds, retirement and the downgrade floor. The reactors pages (`Documentation/reactors/replay.mdx`, `event-processing.mdx`) cover rolling deployments, the warning and filter compatibility; the webhook pages cover narrowing and retirement.

## Options considered

- **Per-type coverage with a debt catch-up job.** Unsound under failed partitions, partial success, handover, repeated cycles and acknowledging narrow clients. Rejected.
- **Typed recovery obligations with one ownership coordinator.** Correct, but a second progress authority inside the shared-cursor engine that decision 0004 replaces. Rejected for this engine.
- **Parking only at recovery,** or **a union without per-target routing.** The set still shrinks in live paths, or a narrow client acknowledges what it cannot handle. Rejected.
- **Refusing a registration with a different type set.** Every handled-event change becomes a stop-the-world release. Rejected for types; kept for subscriber type and filters, against a contract persisted per namespace.
- **Migrating filters or subscriber type in place on the same observer id**, by a stop-the-world release or a quiesce. Stored progress, failed partitions and jobs describe the old filter, and neither a disconnect of every client nor a quiesce says what the new filter owes the history the old one excluded. Rejected for this release: a new observer id; a same-id migration needs its own quiescence and history policy.
- **Fencing capability changes on one connection by delivery epoch.** Needs the client to tag its acknowledgements with the epoch; clients released before this decision cannot. Rejected for fixed capabilities per connection, which costs nothing because the .NET client never changes them on a live connection.
- **Automatic, time-based removal of types.** Cannot tell a finished rollout from a paused one or a scale-to-zero. Rejected.
- **Delivering history for a newly declared type.** Runs side effects for every old event. Rejected.
- **Keeping the #4987 hand-over** (PR #5013) as the long-term answer. Re-applies one client's set and skips the rest's types. Kept only until initialization.
- **Including kernel reactors** with per-silo capabilities. Needs silo-incarnation target identity and liveness for code that ships with the kernel. Rejected.

## Default if unanswered

Events of an added type keep being lost silently whenever versions alternate; clients with different sets keep replacing each other; the observer stalls when the winning client disconnects (until #5013 merges, and afterwards it skips the departed client's types); changing a reactor's handled events keeps needing a separate reactor or a stop-the-world release. The loss is undetectable, and reactor and webhook effects cannot be rebuilt.

## Timeline and scope

The behavior ships together with decision 0011 and is switched on by its cluster gate; slices land dark on `main` as decision 0011's delivery plan states. Retirement, its CLI command and Workbench action are in the same release as the gate, because a narrowing would otherwise park partitions with no exit but fast-forward. Every new-behavior specification is red before its change.

1. **Explicit reader scopes, keeping today's reading.** Every job request and `IObserverKeyIndexes.GetFor` take `Wildcard` or `Explicit(types)` from one per-namespace source (initially today's definition). The type can express an explicit empty scope, but this slice constructs only scopes that read as today: an empty definition or an empty type list in a request persisted earlier is `Wildcard`, as the step fallback, the retry check and MongoDB key enumeration treat it now. Section 8's distinction, an explicit empty scope that starts no reader, is a behavior change that becomes reachable only with an initialized control record (slice 3). *Verify:* specs on InMemory, MongoDB and SQL, including two namespaces with different scopes, and specs pinning the legacy reading of a request without a scope and the wildcard behavior.
2. **State and storage.** The control record with the compatibility contract, the failed-partition cursor, mode and owner, and hold fields in Concepts, Storage and all three providers; the SQL namespace migration in `Source/Kernel/Storage.Sql/EventStores/Namespaces/Observers/Migrations/`, named for the release it lands in, like `v19_15_0.cs` (failed partitions are `StateJson`); the MongoDB mapping; the overlay on every write; and decision 0011's namespace baseline, written on every subscription from this slice on as the only new write before the gate, with the rules for bootstrapping without a baseline and for legacy failed entries (decision 0011, section 9). Nothing else writes the new fields yet. *Verify:* round trips on every provider; the migration on SQLite and PostgreSQL; the baseline written per namespace and kept across a save by the previous release.
3. **Routing, holds and retirement,** behind an initialized control record: per-target capabilities, fixed per connection, run routing, and incompatibility rejection against the contract before any definition is stored; holds and recovery ownership; fences; replay fence clearing; retirement and the all-namespace orchestration with its audit event; applying registrations apart from replay; the kernel warning. The pinning spec `and_another_client_instance_subscribes_with_different_event_types` is inverted. *Verify:* Core.Specs and the integration specs under [Verification](#verification), in `Integration/Client/for_Observers/` and `Integration/Clustering/for_ScaledOutClients/`.
4. **Contracts, Workbench, CLI, client and documentation.** The contracts ship in decision 0011's contracts slice; the Workbench views and the retire action; the CLI against those published contracts; `RegistrationId`, the warning query and log, and the capability-gated missing-handler failure in the .NET client; the documentation in section 9. *Verify:* Workbench specs and the project's Workbench verification workflow; `Integration/Chronicle/for_Observers/` in Cratis/cli; client specs; `bash Documentation/verify-markdown.sh`.

**Out of scope:** kernel reactors, captures and pattern captures; per-target filters or subscriber types; changing the filters or subscriber type of an existing observer id; mixed-version reducers (#4987 for reducers, a follow-up issue); #4583; the ordered-lanes engine (decision 0004); recovering events lost before the upgrade; warnings in clients other than .NET beyond the kernel log and the generated contracts.

## Open questions

None.

Answered:

- **Webhooks and event-store subscriptions stay in scope** (section 1). They are kernel-hosted but user-defined, so a narrowed one parks until the type is retired; treating them like kernel reactors would keep today's silent skip on a remove-and-re-add. Both are side-effecting for decision 0011's legacy settlement.
- **Filter or subscriber-type changes are rejected** against a contract persisted per namespace and checked before anything authoritative is written (section 4). They need a new observer id; a same-id migration is deferred until it has a quiescence and history policy of its own.

## Verification

**Done when:**

- across wide → narrow → wide and narrow → wide → narrow → wide registrations, with and without both versions connected, every event of a member type at or above its fence is delivered to a target that declares it, held, or skipped by an explicit retirement or operator skip;
- no registration, with or without a registration id, shrinks membership;
- the observer stays subscribed while any target remains;
- a partition never delivers a later event before an earlier one it parked at.

**Verify by** these specifications, each red before its change:

- **Rolling upgrade, both orders:** no loss and no duplicate delivery, with the observer behind at the join and with both versions connected.
- **Never-handled type:** no history on any non-replay path.
- **Rollback:** after the wide version leaves, partitions reaching `E` park and are visible in the Workbench, CLI, metric and alert; retiring `E` releases them.
- **Intentional removal:** the narrower registration logs the client and kernel warnings naming the type and the exact command; retirement then removes it.
- **Wide-client disconnect:** the narrow client keeps serving `A` in other partitions; the observer stays subscribed.
- **Concurrent registrations converge:** several registrations during one change produce one commit whatever their order; a retirement and a same-connection re-registration resolve in arrival order as section 3 states.
- **Per-partition order:** `[A1, E2, A3]` in one partition with only an `[A]` target delivers `A1`, parks at `E2`, and never delivers `A3` before `E2`.
- **Original counterexample:** P parks at `E` 50 without delivering `A` 70; the wide target resumes it; `E` 60 on Q is not repeated.
- **Holds and recovery ownership:** resume from the exact position without attempts; a replay hold resumes in replay mode and honors `[OnceOnly]`, and keeps replay mode after a handler failure, its retries and a quarantine; holds never count toward quarantine; global `Next` advances past them; a call from an earlier execution of the same recovery job, or of the same episode, changes nothing.
- **Replay fences:** success clears fences, failure clears none, a partition replay clears none.
- **Key indexes:** on MongoDB and SQL, two namespaces with different memberships each enumerate their own `E`-only partitions.
- **Routing and compatibility:** per-target pins and modes; a registration with different filters or subscriber type is rejected and existing targets stay, also after a silo restart with no target subscribed; the rejected reactor registration stores no definition; the client acknowledges a missing handler against a kernel without the capability and fails it against one with it.
- **Same-connection supersession:** a connection registered `[A, E]` with a run containing `E` selected; the same connection registers `[A]` and is rejected, and the run is delivered to the `[A, E]` stream; after a reconnect with `[A]`, an `E` delivery in flight to the old connection answers `Disconnected` and its partition is retried, routed to a target declaring `E` or parked; nothing is acknowledged without a handler.
- **Webhooks:** narrowing applies URL and authorization changes and notifies subscribers; a pending-retirement type parks; a change of filters is rejected before its definition is stored or any subscriber is notified, in every initialized namespace.
- **Namespace bootstrap:** namespace X registers `[A, E]` and writes it to the shared definition; namespace Y, whose baseline is `[A]`, initializes with members `[A]`, and `E` joins Y only through a change with a fence (decision 0011, section 9).
- **Retirement:** refused while declared; idempotent per operation id, also after a re-declaration and 100 later operations; partial all-namespace results; a resumed orchestration does not re-enumerate; a later namespace is unaffected; replaying the audit event changes nothing.
- **Empty scope:** retiring the last type starts no reader on any provider; once the control record is initialized, an old persisted job with an empty list does not read everything; before that, it reads as today.
- **Legacy clients:** a registration without a registration id widens membership and is routed its types; a narrower one never shrinks membership; the warning query answers `Superseded` and `Unknown` as section 6 states.
- **No hand-over:** with an initialized control record, the winning client's disconnect leaves the narrow client a target with its own types, the definition is not narrowed, and events of the departed client's types park.
- **Unchanged:** reducers, projections, kernel reactors and captures behave as before.
- **Storage and wire:** round trips on InMemory, MongoDB and SQL; the wire-compatibility gates.

## Consequences

- **Easier:** rolling client deployments and rollbacks change a reactor's handled events without loss or a stop-the-world release, and Studio can later supersede the reactor clause of its decision 0020; two client versions share one observer; a disconnect no longer stalls reactors or webhooks; a missing handler shows as a waiting partition, not a silent skip.
- **Harder:** a change that adds or retires a type pauses that namespace's observer (decision 0011); removing a handled type is a deliberate operator step, and partitions receiving it wait visibly until then, which the release note calls out; observer state carries a control record and holds; filter or subscriber-type changes need a new observer id; a connection that changes a reactor's capabilities has to reconnect.
- **Ruled out:** automatic removal of event types; history for newly declared types outside replay; per-type progress in the shared-cursor engine.

## Appendix A: mechanics

### A.1 Hold lifecycle

`o` is the owner `(episode, job, execution, state, revision)`; every transition a recovery job causes requires its `o` to match the entry.

| From | Event | To |
| --- | --- | --- |
| delivery or retry | no connected target declares the event at `p` | waiting (episode `e`, resume `p`, mode `m`) |
| waiting | a declaring target subscribes, or the type is retired | resuming: a new retry job with the next execution number, from `p` in mode `m`; `o` recorded |
| resuming | the job reads to the tail and hands over, with matching `o` | cleared |
| resuming | a run delivered up to `p'` before an uncovered event | waiting (new episode, resume `p'`, mode `m`) |
| resuming | handler failure at `p'` | failing (resume `p'`, mode `m`) with its attempts and retry budget; its retries run in mode `m` |
| failing | retry exhausted, or the observer quarantined | kept with resume position and mode `m` |
| resuming or failing | stopped by a type-set change | resume position harvested (decision 0011); a new job after the commit |
| resuming or failing | restart | a job whose `o` no longer matches is refused by admission and deleted; a new job is started from the durable position |
| any | fast-forward, clearing failed partitions, retiring the last type | cleared and recorded |

### A.2 Storage

- **Control record:** MongoDB `typeSetControl` sub-document written by its own `$set`; SQL JSON column added by the namespace observer migration; InMemory field. It carries the compatibility contract. Holds travel inside the failed-partition entry (SQL `StateJson`, the MongoDB class map, InMemory). Both are nullable additions; an absent record means "not initialized".
- **Namespace baseline:** a nullable sub-document (MongoDB) or column (SQL) of the namespace observer state with the applied event types, subscriber type and filters (decision 0011, section 9).
- **Retirement orchestration:** grain state per event store and operation id, in the existing event-store-level storage.
