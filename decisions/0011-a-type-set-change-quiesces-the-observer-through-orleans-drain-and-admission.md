---
id: 0011-a-type-set-change-quiesces-the-observer-through-orleans-drain-and-admission
title: A change to an observer's event-type set quiesces the observer through the Cratis.Orleans drain and owner admission, and is enabled only once every silo runs that admission
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-17
applies-to:
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/Configuration/Observers.cs
  - Source/Kernel/Core/Configuration/Jobs.cs
  - Source/Kernel/Grpc/Setup/ChronicleServerSiloBuilderExtensions.cs
  - Source/Kernel/Concepts/Observation/**
  - Source/Kernel/Storage/Observation/**
  - Source/Kernel/Storage.InMemory/Observation/**
  - Source/Kernel/Storage.MongoDB/Observation/**
  - Source/Kernel/Storage.Sql/EventStores/Namespaces/Observers/**
  - Source/Kernel/Contracts/Observation/**
  - Source/Workbench/Features/EventStore/Namespaces/Observers/**
  - Integration/Clustering/**
  - Documentation/concepts/observers.mdx
  - Documentation/hosting/configuration/observers.md
---

<!-- markdownlint-disable-next-line MD025 -->
# A change to an observer's event-type set quiesces the observer through the Cratis.Orleans drain and owner admission, and is enabled only once every silo runs that admission

Proposed for Cratis/Chronicle#4955 and Cratis/Chronicle#4987 as the mechanism [decision 0010](0010-observer-event-types-are-the-union-of-registered-definitions.md) uses to apply a change to an observer's members; the two are proposed together and neither is in force without the other. It rests on two Cratis.Orleans guarantees: the drain barrier `StopAndDrain` (v1.20.0, which Chronicle runs) and owner admission (Cratis/Orleans#81, stage 2b, released in v1.21.0). Open points are under [Open questions](#open-questions).

Paths are relative to `Source/Kernel/Core/Observation/` unless they start with `Source/`, `Integration/` or `Documentation/`; paths marked *Orleans* are in Cratis/Orleans at `v1.20.0`, whose `Documentation/concepts/jobs.md` is cited as *jobs.md*. Chronicle line numbers are at `bec5709f8c` (release 19.77.2).

## Context

A reader that runs across a membership change cannot be proven harmless against this code. The event sequence grain advances its in-memory tail after the storage commit (`Source/Kernel/Core/EventSequences/EventSequence.cs:206, 827-831`), so an old reader can pass an event the join position is computed below; old callbacks clear catch-up markers and transition state, not just positions; resumed jobs keep their old type set (`JobsManagerExtensions.cs:124-152`). Decision 0010 therefore needs the reader removed before a change, not reasoned about. The code it has to be removed from:

- **Stopping is not stopped.** Orleans persists `Stopped` before the steps stop. `StopAndDrain` (jobs.md, "Stop a job and wait until it is drained") is the acknowledged barrier, but it does not cover calls nobody awaits, such as the fire-and-forget `CaughtUp` (`Jobs/CatchUpObserver.cs:60-72`), and it does not keep a succeeded step's record: persisting a success deletes the step's state, and after a crash `OnStepCompletedOrStopped` can run twice or not at all.
- **The observer interleaves.** The grain is not reentrant, but about twenty `IObserver` methods are `[AlwaysInterleave]` (`IObserver.cs`), among them the three `CaughtUp` overloads (`:456-476`), `RegisterCatchingUpPartitions` (`:447`) and `FailedPartitionRecovered`, `FailedPartitionNotRecovered` and `FailedPartitionPartiallyRecovered` (`:343-374`). They run at any await of any turn: a `CaughtUp` clears the catch-up markers (`Observer.Catchup.cs:170`), awaits its state write (`:181`) and then enters `Routing` (`:186-189`).
- **Checkpoints are debounced and steps are many.** A step's checkpoint becomes durable every `StepCheckpointBatchInterval` batches, 100 by default (`Source/Kernel/Core/Configuration/Jobs.cs:51`), except for collapsing projections (`Jobs/HandleEventsForPartition.cs:69`); a reactor catch-up has one step per partition (`Jobs/CatchUpObserver.cs:115-172`).
- **Orleans 1.20 does not ask the owner on every path.** It resumes a job left `Running` or `StartingSteps` without calling `CanResume` (*Orleans* `Source/Orleans/Jobs/Job.cs:176-226`, `:396`), and advances stages without asking.
- **Unsubscribing pauses jobs.** `Unsubscribe` stops every non-replay job of the key (`Observer.cs:455-461`, `PauseJobs` `:1034-1048`), and only `ResumeJobs` (`:1155-1181`) resumes them.
- **Every labeled merge to `main` releases,** so a multi-slice change ships as several releases.

## Decision

A change that adds a type to, or retires a type from, a namespace observer's members (decision 0010) runs as a **quiesce**: the observer stops admitting work, drains every unfinished job of its key with `StopAndDrain`, harvests positions from Chronicle's own progress records into per-partition rows, reads the committed tail from storage as the join position, and commits the next revision; only then does work resume. Every call from a job carries the revision it was created under and cannot move observer state across the change. Owner admission in Cratis.Orleans 1.21 keeps every job of an older revision from running again. The quiesce, and decision 0010 with it, are enabled only on a cluster where every silo runs a Chronicle release that registers admission, and observers are initialized without a quiesce.

### 1. Entering a change

- **Triggers:** a registration that declares a type that is not a member, and a retirement that passes its checks (decision 0010, sections 3 and 6). Initialization is not a change (section 9).
- **Latch first.** In the turn that starts the change, before its first await, the observer sets an in-memory latch and its running state to `ChangingDefinition`. It then writes durably the pending change, the running state, the committed tail `J0` (section 4) and, once that intent is stored, the ids of the key's unfinished jobs.
- **Stop admitting work.** The queue subscription is removed (leaving `Observing`), and `Handle` already drops batches while the observer is not `Active` (`Observer.Handling.cs:394`). Catch-up, partition recovery, retry reminders and replay requests are refused, and admission refuses the key's jobs (section 6). The watchdog only alerts: no unsubscribe on disconnect (`Observer.Watchdog.cs:170-212`; decision 0010's target removal still applies), no job rescue (`CheckJobTasks`, `:217-262`), no fast-forward (`:343-369`).
- **Quarantine does not defer a change.** A quarantined observer runs the change the same way: `StopAndDrain` drains jobs that are already stopped, and replays, which `PauseJobs` leaves running, are drained too. After the commit it stays `Quarantined`, and resumes when the quarantine ends. Deferring the change until then would put every event of an added type appended meanwhile below its fence, for as long as the quarantine lasts.

### 2. Draining

- A non-interleaving grain timer ticks every `Observers.DefinitionChangePollInterval` (1 second by default) and calls `IJobs.StopAndDrain(key, jobId, TimeSpan.Zero)` for each recorded job not yet drained or gone. The observer never awaits a drain with a positive timeout: step work and job hooks call the observer's non-interleaving `ReportHandledEvents` and `PartitionFailed`, so a waiting drain and a waiting step would block each other (jobs.md, "Do not wait for a drain from a grain your job calls back into").
- `Drained` and `Completed` count as drained and `NotFound` as gone; `TimedOut`, `UnknownJobType` and an error are polled again on the next tick. Polling only the recorded ids keeps a tick from listing every unfinished job in the namespace. When all are drained, the key's unfinished jobs are enumerated once more; a job found then, whose start raced the intent, is added and drained.
- `Drained` means no step's work or activation callback runs any more, each step's outcome and last checkpoint is in step storage, and the job's hooks have run. A step does not pass its cancellation into the subscriber wait (`Jobs/HandleEventsForPartition.cs:435`, `ObserverSubscriberExtensions.cs:33-59`), so it finishes the wait it is in, with an answer or the subscriber timeout, before it stops; a drain never abandons a subscriber wait.

### 3. Progress records and the harvest

- **When a step writes one.** A step of an observer whose control record is initialized writes a progress record (observer key, job id, partition, position, outcome) when its checkpoint has become durable (the debounced write in `ReportNewSuccessfullyHandledEvent`, `Jobs/HandleEventsForPartition.cs:101`) and at its terminal outcome: success, stop or failure. That is one write per durable checkpoint and one per outcome, not one per batch.
- **A succeeded step's position** is the one it reports with success. Its record is written before it reports; if that write fails, the step does not report success and closes with a partial result, as a refused checkpoint callback already does (`ReportFromWork`, `ClosedResult`, `:404-417`). `Drained` guarantees every outcome of a drained step was reported, so its terminal record exists.
- **The harvested position** of a partition is the later of its record and its step's checkpoint in step storage.
- **Every record of the key is harvested,** not only those of the recorded jobs, so a job that completed just before the change, whose `CaughtUp` the change turns into a no-op (section 5), loses nothing. Records are deleted when the observer applies their job's completion, after the commit that harvests them, or by the sweep. A crash between applying a completion and deleting its records can make a partition deliver again, which at-least-once after a crash already allows.
- **A failed entry beats a continuation.** A partition with a failed-partition entry, a failure or a hold, gets no continuation: a step that handled up to 70 and failed at 71 resumes from its entry at 71, as `ProbeAndStartCatchupJob` already skips failed partitions (`Observer.Catchup.cs:281`). A terminal failure record becomes a failed entry at its position when the observer has none, so a `PartitionFailed` still on its way adds nothing. A recovery job's record moves its entry's resume position only when the owner matches (decision 0010, section 5).
- **What a record becomes.** Catch-up and partition catch-up records become partition **continuations**; retry and hold-resume records become the failed partition's resume position. Replays are not harvested; each becomes a **restart plan**: its scope (the whole observer or one partition), its bounds, its event types (a full replay takes the new member set; a partition replay keeps its requested types that are still members) and the id its replacement job will have.
- **Rows, not one document.** Continuations and restart plans are rows keyed by observer, revision *r + 1* and partition or plan, written over successive ticks, at most 1,000 per tick, so neither a turn nor a stored document grows with the partition count. Readers take rows of the current revision only; rows of any other revision are ignored and removed. A crash mid-harvest re-enters from the intent and rewrites rows by key.
- **Sweep.** Outside a change, the watchdog tick removes progress records whose job no longer exists and that are older than the Orleans response timeout. The completion call they were waiting for has failed by then, and the watchdog's catch-up rescue recovers that as it does today.

### 4. Commit and resume

- **The commit turn** runs on the timer once every row is written. Before its first await it advances the in-memory revision to *r + 1*, so a revision-*r* call that resumes at any later await finds a newer revision (section 5). It waits for any observer state write already in flight, so a write prepared under revision *r* cannot land after the commit's.
- **Join position.** It reads the committed tail from storage (`IEventSequenceStorage.GetTailSequenceNumber`, `Source/Kernel/Storage/EventSequences/IEventSequenceStorage.cs:220`) on the primary: MongoDB primary read preference with majority read concern, the SQL primary. Each added type's fence is `J = tail + 1`, or `EventSequenceNumber.First` when the sequence is empty (`Unavailable` survives addition, `Source/Kernel/Concepts/Events/EventSequenceNumber.cs:15-20, 97`).
- **One write** of the control record publishes revision *r + 1*, the members with their fences, the registration identities and retirement results, and clears the pending change (decision 0010, section 2). Then the old jobs and the harvested progress records are deleted.
- **Missed events are counted, not owed.** Events of an added type with sequence numbers from `J0` up to `J` were appended while the change ran and lie below the fence. When there are any, a warning alert gives their count and recommends a replay; no per-type debt is recorded.
- **Resume.** Restart plans first: each starts its job with `IJobs.Start(key, jobId, request)` (*Orleans* `Source/Orleans/Jobs/IJobs.cs:34`) using its recorded id, so starting it again after a crash finds the job and starts nothing; the row is removed once the job is stored. Then the queue is subscribed with the new set and revision; catch-up runs from `Next` and excludes partitions with continuations, which get partition catch-ups from their continuation (`Observer.Catchup.cs:313-321`); retries run from their failed partitions; holds whose awaited type is now declared or retired resume. An observer that is quarantined stays so and resumes when the quarantine ends.

### 5. Calls from jobs carry the revision

- Job requests, live queue batches and every call from step work or a job hook to the observer carry the revision they were created under; a job without a revision counts as revision 1 (section 9).
- **During a change** the job calls that advance positions or clear state are no-ops, logged: the three `CaughtUp` overloads, `RegisterCatchingUpPartitions`, `FailedPartitionRecovered`, `FailedPartitionPartiallyRecovered` and `FailedPartitionNotRecovered`. The harvest carries what they would have carried. `PartitionFailed` and `ReportHandledEvents`, which do not interleave, still apply; a transition they would trigger waits for the resume.
- **Interleaving callbacks re-check after every await.** If the latch has moved since the call entered, because a change began or the revision advanced, the callback stops without a further mutation or transition. What it did before its first await belonged to the revision that was current then. A `CaughtUp` that cleared markers before a change and resumes after its commit therefore never reaches `TransitionTo<Routing>()` and cannot clear the markers of revision *r + 1*'s partition catch-ups (`Observer.Catchup.cs:317`).
- **After the commit** a call or batch of an older revision is ignored entirely: it advances no position, clears no marker and triggers no transition.

### 6. Owner admission (Cratis.Orleans 1.21)

A drain stops a job; it does not decide whether the job may run again. Chronicle uses the admission API of Cratis/Orleans#81, stage 2b:

- `IJobAdmission.MayRun(JobAdmissionRequest(JobKey, JobId, JobType, IJobRequest), CancellationToken)` returns `Task<bool>` and is registered with `siloBuilder.AddJobAdmission<T>()`, a singleton added with `TryAddEnumerable`. A 1.21 silo without the registration admits everything.
- It is asked at `Start` (refusal: `StartJobError.NotAdmitted`, 7), at every `Resume` — explicit, rehydration of a job left running or starting, and recovery — (refusal: `CannotResumeJobError.NotAdmitted`, 3; `IJobsManager.Resume` returns false) and at every stage advance. A preparing job is not asked on `Resume`, which starts nothing there; its `Start` was asked.
- Refusing a `Running` or `StartingSteps` job makes `Resume` write `Stopped` with a pending drain and answer `NotAdmitted` at once; the drain completes in the job's own timer turn. If that drain times out, the job stays stopped with the drain pending until the owner's `StopAndDrain` or its next activation; nothing restarts it. A refused stage advance stops the job and runs `OnStopped`.
- A refusal is final for that attempt; recovery and rehydration go through `Resume`, which asks again, so a job stays refused for as long as the owner refuses.
- Throwing, being cancelled or exceeding the time limit (the 1.20 drain bound, about half the Orleans response timeout) fails the operation and changes nothing; it is never a refusal. Rehydration leaves the job `Running` in storage and asks on the next pass; a stage advance retries the check on a timer.
- There is no per-step check, and none is needed: step work starts only from the fan-out of an admitted `Start`, `Resume` or stage start, and a restart after an earlier stop or a deferred start happens only inside an admitted execution that has not been stopped. A job type with `StartStepsInBackground` is asked in `Start`'s turn and fans out on the next timer tick; only `Stop`, `StopAndDrain` or `Remove` can run in between, and each makes that tick a no-op.
- `MayRun` never calls the job, its steps, or a non-interleaving method of a grain that may be awaiting that job's `Start` or `Resume`.

Chronicle's `MayRun` reads the control record from storage on the primary and calls no grain. It admits a job only when its request carries the current revision, no change is pending, and, for a recovery job, its owner matches the failed-partition entry. The observer never awaits a refusal's drain in its own turn: `ResumeJobs` and the revision filter in `JobsManagerExtensions.cs:124-152` skip jobs of other revisions, so a job that admission would refuse while running reaches `MayRun` only through rehydration, outside the observer. Those filters, `CanResume` overrides that read the control record, and deleting old jobs on sight are defense in depth; on their own they leave the paths Orleans 1.20 does not route through `CanResume` unguarded.

### 7. Why no reader crosses a change

Once a job answers `Drained`, none of its steps reads, delivers or checkpoints again, and admission keeps it and every other job of revision *r* from running. `J` is read only after every job of the key has drained, so every position a revision-*r* reader scanned or passed held an event already committed and lies below `J`; whatever it skipped of an added type is history below the join, which decision 0010 excludes. Reading the committed tail from storage on the primary is what makes this hold: an event committed but not yet in the grain's in-memory tail is below `J`, and a lagging secondary cannot put `J` below an event an old reader passed. Appends are numbered and committed serially by the event sequence grain (`Source/Kernel/Core/EventSequences/EventSequence.cs:793-831`), so an append in flight during the tail read gets a number at or above `J` and is delivered. Every reader after the commit runs with revision *r + 1*, the full member set and the fences, and section 5 keeps a late revision-*r* call from moving anything. That the harvest spans several ticks changes none of this: nothing it reads can change once the jobs are drained.

### 8. Bounded, observable, recoverable

- **Pause.** With jobs running, a drain settles within the subscriber timeout (`Observers.SubscriberTimeout`, 30 seconds by default, `Source/Kernel/Core/Configuration/Observers.cs:34`) plus `JobsOptions.StepStopTimeout` (10 seconds) of being requested, and the next tick sees it; the harvest adds a tick per 1,000 rows. A subscriber timeout at or below zero waits indefinitely, and a drain can stay stuck, for example on a step whose silo cannot drain; then only forced completion ends the pause.
- **Visible.** `Observers.DefinitionChangeTimeout` (5 minutes by default) raises a warning alert when exceeded while polling continues. The running state, the pending change, its age and the jobs still draining appear in the Workbench and the CLI; metrics `chronicle-observer-definition-changes-in-progress` (gauge) and `chronicle-observer-definition-change-duration` (histogram); entry and exit logs carry the revision.
- **Forced completion.** **Complete definition change** with `--force` deletes the remaining jobs without harvesting them and commits; their unharvested progress is delivered again, and an undrained step could deliver out of order until it settles, so it is the one path that weakens per-partition order. It uses the kernel's existing authorization, which admits any authenticated client including applications (`Source/Kernel/Core/Sequences/ReopenStreamScope.cs:26`); it requires a reason and appends an audit-only system event recording who forced it, when and why, as retirement does, and the Workbench shows that event. Restricting kernel administration operations to an operator role is Cratis/Chronicle#5015.
- **Crashes.** Before the commit the change re-enters from the durable intent; a job already asked to drain completes its drain when next activated and refuses `Resume` until then; progress records and harvested rows survive. After the commit, jobs and progress records of older revisions are deleted on sight and restart plans run first. No path needs a manual storage edit.
- **Abandoned subscriber calls** are outside the drain. The subscriber timeout abandons the kernel's wait, not the subscriber's work (`ObserverSubscriberExtensions.cs:26-58`, `Reactors/Clients/ReactorObserverSubscriber.cs:56-60`), so a drained observer can still have client work outstanding from a wait abandoned before or during the drain. That partition failed, and its retry after the commit can overlap the abandoned call exactly as a retry after a timeout does today. This decision neither creates nor widens that overlap; it is tracked in Cratis/Chronicle#5014.

### 9. Rollout

- **The cluster gate.** A silo of the gate release registers a marker grain type and `AddJobAdmission<T>()` together in `AddChronicleToSilo` (`Source/Kernel/Grpc/Setup/ChronicleServerSiloBuilderExtensions.cs:48`), and a spec asserts that registering the marker registers admission. That release builds on Cratis.Orleans 1.21, so a silo carrying the marker runs 1.21 or later with admission registered. Each silo reads from the Orleans cluster manifest whether every active silo carries the marker; Chronicle has no silo-capability mechanism of its own (`Source/Kernel/Contracts/Clients/KernelCapabilities.cs` is client-facing). The check needs a complete view of active membership and fails closed: a silo whose manifest is missing counts as older. The quiesce is therefore enabled only once Cratis.Orleans 1.21 has shipped, every silo runs it, and every silo registers admission.
- **Before the gate opens** every silo keeps today's behavior for type changes: the registration replaces the set; no control record, hold, fence or progress record is written; retire type and complete definition change answer `ClusterUpgradeInProgress`. The gate opens only initialization and is not re-evaluated afterwards.
- **Delaying the gate.** A silo with `Observers.HoldTypeSetGate` set registers admission but not the marker, so the cluster counts it as older and the gate stays closed until the setting is cleared on every silo. It is off by default and lets an operator keep a rollback cheap until the rollout is final.
- **Initialization does not quiesce.** Once the gate is open, the next registration of each namespace observer writes revision 1 directly: the members are the stored types, with no fences. It changes no set, so the ordinary subscribe path resumes the jobs that unsubscribing paused through `ResumeJobs`, as today. A job without a revision counts as revision 1 and is admitted while revision 1 is current and no change is pending. When that registration also declares a type that is not a member, a change starts after the record is written.
- **The first real change after the upgrade** treats unrevisioned jobs by observer kind. For a replay-safe observer (projection, reducer or read model) it drains them like any other job and harvests the records and checkpoints they wrote since the upgrade; a step that succeeded under the previous release left no record and Orleans deleted its state, so its partition is delivered again from `Next` or its failed-partition position: a one-time redelivery, limited to that change. A reactor or webhook is never redelivered silently, because its side effects are not idempotent (Cratis/Chronicle#4809): the change waits for its unrevisioned jobs to finish instead of draining them. It resumes a legacy job that unsubscribing paused, admits its callbacks as revision 1 while the change is pending, and commits once the job has completed and reported its positions. Its pause therefore lasts as long as that job, and `DefinitionChangeTimeout` raises the alert as for a stuck drain. Replays restart from plans.
- **Delivery.** Slices land on `main` dark: schema additions are additive and nullable, nothing new is written, and new paths stay unreachable until the gate. The order: (1) decision 0010's reader scopes; (2) state and storage for both records, including the progress-record and row stores, the `ChangingDefinition` and `WaitingForHandler` members, and the downgrade refusal below; (3) contracts, released as a minor so Cratis/cli builds against published packages, with their operations answering `ClusterUpgradeInProgress`; (4) decision 0010's routing, holds and retirement; (5) this record's engine on Cratis.Orleans 1.21, with admission registered; (6) Workbench, the CLI release, the .NET client and documentation; (7) the gate, last.
- **The downgrade floor** is the gate release, the first that writes new-format state; the release note states it and names the minimum CLI version. A silo of any release from slice 2 on that finds an initialized control record refuses to activate the observer and logs that the kernel is below the floor. Releases before slice 2 cannot be changed: an older silo rewriting a failed partition drops hold fields, and the new enum members are unknown to it. An older silo joining after initialization is a downgrade below the floor and is unsupported.
- **During the rollout** nothing is worse than today and no new-format state is written, so older silos never read it. Not guaranteed until the gate opens: no loss across a type change, the #4987 fix, and recovery of events skipped while the rollout lasts; those losses stay undetectable, as in decision 0004 Q7. Once the gate is open, rolling client versions are covered in any order.

## Options considered

- **Readers that run across the change with a revision cutover.** Not provable against the in-memory tail and callback side effects. Rejected.
- **Job status as the drain barrier.** Orleans persists `Stopped` before the steps stop. Rejected for `StopAndDrain`.
- **Awaiting a drain inside the observer's turn.** Step work and job hooks call the non-reentrant observer, so the two block each other. Rejected for polling with `TimeSpan.Zero` from a timer.
- **One non-interleaved commit turn as the guard against late callbacks.** `[AlwaysInterleave]` callbacks run at any await of it. Rejected for no-ops during the change, the synchronous latch and re-checks after every await.
- **A progress record per handled batch.** About a hundred times the step writes of every reactor and webhook job, permanently, for a rare event. Rejected for records at durable checkpoints and outcomes.
- **Retaining succeeded step outcomes in Cratis.Orleans** (opt-in retention with a step-outcomes query). Would make progress records unnecessary, but is not in 1.21. Deferred to an Orleans issue if the records prove costly.
- **Continuations inside the control record.** One document and one turn grow with the partition count. Rejected for per-partition rows.
- **Deferring a quarantined observer's change until the quarantine ends.** The window below the fence becomes unbounded and silent. Rejected.
- **Initialization as a quiesce.** Every observer would wait out its timeout at the end of the rollout, all at once. Rejected.
- **Waiting for legacy jobs at the first change for every observer.** Avoids the one-time redelivery everywhere, but makes every observer's first change wait out its longest legacy job. Taken only for reactors and webhooks (section 9), whose side effects must not repeat; replay-safe observers take the redelivery.
- **Enabling the quiesce on Chronicle's own resume guards before Orleans admission.** Covers only the resume paths that reach `CanResume` or Chronicle's code. Rejected.
- **A bridge minor with an activation marker.** Enforces the floor in software but adds a release. Rejected for a documented floor; the dark slices give a downgrade refusal without an extra release (open question 3).
- **Fencing abandoned subscriber calls.** Would hold every retry, not only a change, until the client answers or disconnects: delivery fencing for the whole engine. Left to #5014 and decision 0004.

## Default if unanswered

Decision 0010 cannot be enabled, so events of an added type keep being lost silently whenever versions alternate and clients with different sets keep replacing each other. Enabling 0010 on a weaker mechanism would let a reader cross the change and skip what this record guarantees it cannot.

## Timeline and scope

The record ships with decision 0010 in the delivery order of section 9, and its engine slice cannot merge before Chronicle takes a Cratis.Orleans 1.21 release. Every new-behavior specification is red before its change.

1. **State and storage** (in slice 2): the intent fields, `ChangingDefinition`, the progress-record store, the continuation and restart-plan rows, and the downgrade refusal, on every provider. *Verify:* round trips on InMemory, MongoDB and SQL; the refusal spec in [Verification](#verification).
2. **Contracts** (slice 3): complete definition change and the definition-change state in the observer's read model.
3. **Engine** (slice 5): entering a change, drain polling, progress records, harvest, commit and resume, revisions on jobs, callbacks and batches, the callback rules of section 5, Chronicle's `MayRun`, forced completion with its reason and audit event, metrics and alerts. *Verify:* Core.Specs and integration specs in `Integration/Client/for_Observers/` and `Integration/Clustering/`.
4. **Gate** (slice 7): the marker grain type with admission, the gate check, `HoldTypeSetGate`, initialization and legacy-job handling. *Verify:* the gate and initialization specs.

**Out of scope:** fencing abandoned subscriber calls (#5014); retaining succeeded step outcomes in Cratis.Orleans; an automated rollout with an older Chronicle binary in the same cluster.

## Open questions

1. **A membership change restarts a running replay.** The replayed prefix is delivered again in replay mode. Deferring the change until the replay ends would instead exclude events of the added type appended meanwhile. *Recommendation:* restart; replay is already a redelivery.

Answered:

- **Forced completion** uses the existing authorization with a required reason and an audit event shown in the Workbench; operator roles are Cratis/Chronicle#5015 (section 8).
- **The downgrade refusal** from slice 2 on is taken: the gate release is the downgrade floor, stated in its release note; there is no bridge release (section 9).
- **`HoldTypeSetGate`** ships as a rollout-only setting, off by default and documented with when to use it (section 9).
- **The first change after the upgrade** redelivers once only for replay-safe observers; reactors and webhooks wait for their legacy jobs instead (section 9).

- **The subscriber-timeout overlap** is accepted because it is pre-existing and this decision does not widen it; it is tracked in Cratis/Chronicle#5014 (section 8).
- **The quiesce waits for Orleans owner admission:** it stays disabled until Cratis.Orleans 1.21 ships, every silo runs 1.21 or later, and every silo registers `AddJobAdmission` (section 9).

## Verification

**Done when:**

- no reader of an older revision runs or advances progress after a commit, and no job of the key runs while a change is pending;
- registration transitions add no duplicate delivery, except through forced completion and the one-time redelivery of section 9; at-least-once redelivery after a crash or timeout is unchanged, because subscriber effects precede the durable outcome (`Observer.Handling.cs:255, 331-357`); a delivery abandoned by the subscriber timeout may overlap its partition's retry, as today (#5014);
- a change that cannot drain is visible and completes through an operator command without editing storage;
- while any silo lacks the marker, behavior matches today and nothing new is written.

**Verify by** these specifications, each red before its change:

- **Drain barrier:** a step still writing its checkpoint when its job is persisted `Stopped` is not harvested until `StopAndDrain` answers `Drained`; `TimedOut` is polled again on the next tick; the observer never awaits a drain inside its turn; a step's subscriber wait finishes before it stops.
- **Interleaving callbacks:** a `CaughtUp` suspended at its state write when a change begins, resuming after the commit, neither enters `Routing` nor clears revision *r + 1*'s markers; `RegisterCatchingUpPartitions` and the three recovery callbacks change nothing during a change; a revision-*r* call resuming at any await of the commit turn changes nothing.
- **Stale results:** a `CaughtUp`, retry completion, `ReportHandledEvents`, `PartitionFailed` or queue batch of an older revision arriving after the commit changes nothing, and its loss leaves no partition repeated.
- **Harvest:** a catch-up stopped mid-partition continues from its last durable position; a partition whose step succeeded, with its step state deleted and `OnStepCompletedOrStopped` never run, is not repeated; the same for a retry; a job that completed just before the change, whose `CaughtUp` became a no-op, loses nothing; a step that handled 70 and failed at 71 resumes from its entry at 71 with no continuation; a failed terminal-record write closes the step instead of reporting success; a step writes one record per durable checkpoint and one per outcome.
- **Large key:** a harvest of 100,000 partitions spreads over ticks, the control record's size does not depend on the partition count, and registrations are handled between ticks.
- **Join position:** an `E` event committed to storage but not yet in the grain's tail lies below `J`; an append in flight during the tail read is delivered; an empty sequence fences at `First`; the tail is read from the primary.
- **Missed events:** `E` events appended between `J0` and `J` raise the alert with their count.
- **Quarantine:** a change while quarantined drains, commits with its fences at that commit's `J`, stays quarantined, and resumes when the quarantine is cleared.
- **Watchdog:** during a change it neither unsubscribes, rescues jobs nor fast-forwards; it alerts.
- **Admission,** against Cratis.Orleans 1.21: an old-revision job is refused on explicit `Resume`, on rehydration while `Running` (stopped with a pending drain, no step work), and at a stage advance; a job of the current revision is refused while a change is pending; a throwing `MayRun` leaves the job unchanged and not running; the observer never awaits a refusal in its turn.
- **Replay restart:** a change during replay restarts it with the new set; a bounded, selected-type partition replay restarts with its bounds and remaining types; a crash after the commit, before or after the replacement job is stored, starts it exactly once.
- **Bounded and recoverable:** a subscriber that never answers stops being waited for at the subscriber timeout and its partition fails; a job that never drains raises the alert after `DefinitionChangeTimeout`; forced completion is refused without a reason, and with one appends its audit event and commits; a silo crash before, during and after the commit converges, including a crash mid-drain, which Orleans completes on reactivation without resuming the job.
- **Gate:** `AddChronicleToSilo` never registers the marker without admission; `HoldTypeSetGate` keeps the gate closed; with an older silo present, or a silo whose manifest is missing, behavior matches today (including the #4987 hand-over) and nothing new is written; after it leaves, observers initialize on their next registration. `ClusteringFixture` runs one binary in process (`Integration/Clustering/ClusteringFixture.cs:22-33`), so an older silo is simulated by a silo without the marker and admission; a rollout with a real older binary is not covered by these specifications.
- **Initialization:** observers re-registering at the end of the rollout do not enter `ChangingDefinition`; a legacy catch-up paused by the reconnect resumes and completes as revision 1; at the first change a projection's legacy job is drained and only partitions whose step succeeded before the upgrade are delivered again, while a reactor's legacy job is resumed and awaited and nothing is delivered again; a legacy replay restarts from a plan.
- **Downgrade refusal:** a silo of a slice-2 release that finds an initialized control record refuses to activate the observer. This specification passes or fails.
- **Sweep:** removes records of vanished jobs older than the response timeout, and never during a change.

## Consequences

- **Easier:** decision 0010's changes are applied without a reader crossing them, and the argument rests on two engine guarantees with their own specifications rather than on timing.
- **Harder:** a change pauses that namespace's observer while it drains; the release waits for Cratis.Orleans 1.21; jobs, callbacks and queue batches carry revisions; steps write progress records; the rollout completes only when every silo runs the gate release; the gate release is a downgrade floor; a reactor's or webhook's first change after the upgrade waits for its legacy jobs.
- **Ruled out:** readers that span a membership change; enabling the quiesce before every silo runs owner admission.

## Appendix: definition-change state

| From | Trigger | To |
| --- | --- | --- |
| any running state | registration adding a type, or a retirement that passes its checks | `ChangingDefinition`: latch set, then intent, `J0` and job ids written |
| `ChangingDefinition` | another registration or retirement | `ChangingDefinition`, merged intent written |
| `ChangingDefinition` | timer tick with jobs not drained | `StopAndDrain(TimeSpan.Zero)` for each recorded job not yet drained or gone |
| `ChangingDefinition` | every recorded job drained | one more enumeration; new jobs added and drained; then harvest rows over ticks |
| `ChangingDefinition` | every row written | commit turn: revision latched, tail read, one write; old jobs and records deleted; restart plans; `Routing`, or `Quarantined` if it was |
| `ChangingDefinition` | `DefinitionChangeTimeout` exceeded | polling continues, warning alert |
| `ChangingDefinition` | forced completion | remaining jobs deleted unharvested, commit, audit event |
| `ChangingDefinition` | silo restart | re-entered from the durable intent; pending drains complete on the jobs' reactivation |
