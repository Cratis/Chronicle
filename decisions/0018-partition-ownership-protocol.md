---
id: 0018-partition-ownership-protocol
title: Give every observer partition one durable owner that every writer checks, hand it over only after the previous writer has drained, and enforce it only behind a compatibility floor
status: proposed
stage: none
class: contract
reversibility: costly
proposed: 2026-10-24
decider: TBD (woksin / Einar)
applies-to:
  - Source/Kernel/Core/Observation/**
  - Source/Kernel/Core/EventSequences/AppendedEventsQueue.cs
  - Source/Kernel/Storage/Observation/**
  - Source/Kernel/Storage*/**
  - Documentation/**
---

<!-- markdownlint-disable-next-line MD025 -->
# Give every observer partition one durable owner that every writer checks, hand it over only after the previous writer has drained, and enforce it only behind a compatibility floor

Proposed on 2026-10-24; the decider is to be named (woksin or Einar). This record settles how the kernel decides who may deliver events to one observer partition. It is the prerequisite for partition rewinds ([#4334](https://github.com/Cratis/Chronicle/issues/4334)) and the remaining replay work under [#5260](https://github.com/Cratis/Chronicle/issues/5260), and it carries the observer-side part of rolling upgrades ([#5169](https://github.com/Cratis/Chronicle/issues/5169)). It adopts the Cratis.Orleans drain and owner admission that [decision 0011](0011-a-type-set-change-quiesces-the-observer-through-orleans-drain-and-admission.md) relies on, and follows [decision 0017](0017-generation-enablement-compatibility-and-recovery.md) in gating a new protocol behind an enforceable floor. It does not change [decision 0016](0016-materialization-binding-ownership-and-fencing.md): that record fences writes into a materialization; this one decides which delivery path may produce them. Partition rewinds stay governed by [decision 0002](0002-event-generation-preservation-and-delivery.md) for generation selection, revision and redaction.

This record is scoped to today's delivery engine, the shared observer cursor. [Decision 0004](0004-ordered-lanes-observer-delivery.md) proposes replacing that engine with ordered lanes and durable per-key obligations; it is proposed, not accepted, and nothing of it is implemented. 0018 is compatible with 0004 but independent of it, and is meant to be superseded by it: a partition claim is a writer-exclusion rule, not a progress authority, so it adds nothing [decision 0010](0010-observer-event-types-are-the-union-of-registered-definitions.md) ruled out for this engine, and it is retired for a cluster once that cluster finalizes ordered lanes. See [Relationship to decision 0004](#relationship-to-decision-0004).

The mixed-version section is co-authored with the rolling-upgrade work and carries explicit points for it to confirm.

## Context

Seven writers can deliver events to one observer partition today, and each decides on its own whether it may:

| Writer | Where it decides |
| --- | --- |
| Live delivery (appended-events queue into the observer) | The observer's handling gate: not failed, observer active, not preparing catch-up, partition not in the replaying or catching-up sets. |
| Partition catch-up job | Started by the observer after adding the partition to the catching-up set; does not ask again while it runs. |
| Partition replay job | Started after adding the partition to the replaying set and recording where live delivery was held back. |
| Observer-wide catch-up and replay jobs, including catch-up started when the appended-events queue spills an observer | Skip the partitions that were failed when the step began; do not look at partition replay or catch-up holds. |
| Failed-partition retry and the webhook probe | Started per failure episode; do not look at partition replay holds. |
| Watchdog and routing recovery | Clear both hold sets when the expected job looks missing and route the observer again. |
| Abandoned webhook deliveries | Held by a time-bounded delivery fence until the remote call can no longer land. |

There are therefore five overlapping notions of "this partition is taken": the replaying set, the catching-up set, the failed-partition store, the delivery fences, and an in-memory ownership ticket used for skip detection. None of them records *which* writer holds the partition, and none is checked by every writer.

An attempt to add partition rewinds on top of this (a four-state handoff per partition replay) was reviewed and found unsound. The review traced most of the failures to behavior that already exists on main, now filed as:

1. [#5378](https://github.com/Cratis/Chronicle/issues/5378): a job in `Removing` counts as finished while its steps are still delivering.
2. [#5379](https://github.com/Cratis/Chronicle/issues/5379): the watchdog only recognizes observer-wide catch-up jobs, so a partition catch-up looks missing; routing then clears every hold and live delivery resumes beside the still-running job.
3. [#5380](https://github.com/Cratis/Chronicle/issues/5380): observer-wide catch-up and failed-partition retries deliver to a partition a partition replay holds.
4. [#5381](https://github.com/Cratis/Chronicle/issues/5381): catch-up after a selected-type or refused partition replay can start past events held back during it (event loss).
5. [#5382](https://github.com/Cratis/Chronicle/issues/5382): a delayed job start can land after the observer concluded no job exists, and a caller-chosen job id stops deduplicating once the job's state is deleted.
6. [#5383](https://github.com/Cratis/Chronicle/issues/5383): partition replay completion writes observer state and failure state separately.
7. [#5384](https://github.com/Cratis/Chronicle/issues/5384): during a rolling upgrade an observer can reactivate on an older silo that does not know a new protocol.

Two further facts shape the choice. Orleans single activation and job completion do not by themselves exclude a stale writer: a step can still be delivering after its job is marked stopping, and a remote webhook can still land after its delivery timed out. And per-writer fences, the approach the rewind branch took, would add a sixth notion of "taken" and still leave the seven gaps open.

If nothing is decided, partition rewinds stay blocked, the seven defects get fixed one guard at a time with no shared invariant, and every new delivery path (#5260, rolling upgrades) must rediscover which holds it has to respect.

## Decision

Each observer partition has at most one **owner**, recorded durably in the observer's state document as a **partition claim**. A partition with no claim is owned by live delivery. Every writer, live delivery included, delivers only while it holds the current claim, and ownership moves only by a claim transition that the observer computes and that is persisted before the next owner starts. A claim is handed to a new owner only after the previous owner has **drained**. Enforcement switches on only behind a compatibility floor; until then the claim is written and checked in shadow.

### The claim

A claim records:

- **Holder**: the kind of writer (partition replay, partition catch-up, failure recovery, observer-wide job) and the job that acts for it.
- **Epoch**: a number that increases on every transition for the partition and is never reused.
- **Resume position**: where the holder must start reading. It is copied, when the claim is taken, from the progress the observer already keeps (the partition's last handled event, or the point from which live delivery was held back), and it is never later than an event held back from live delivery. It is not an acknowledgement: progress and completion keep coming from the observer's existing cursor and reports, so the claim is not a second progress authority.
- **Pending work**: requests that arrived while the partition could not be granted (for example a partition replay requested during an observer-wide catch-up), granted in order when it can be.

The replaying and catching-up sets remain in the state document as views derived from the claims, so readers that only know them keep working.

### Who may write

- **Live delivery** delivers to a partition only when it has no claim, the observer is active, and no observer-wide job owns delivery.
- **A job** carries its partition, its holder kind and its epoch. Before each delivered batch it asks the observer whether that epoch still holds the claim; the question interleaves and reads memory only. Job admission (`IJobAdmission`) refuses to start or resume a job whose epoch is not the current one, so a delayed start, a rehydrated job or a reused job id after state deletion cannot run (#5382).
- **Observer-wide jobs** and **partition claims** exclude each other. An observer-wide catch-up leaves out every claimed partition when it prepares, and no new partition claim is granted until it reports back; requests that arrive meanwhile become pending work (#5380). Starting an observer-wide replay first revokes and drains every partition claim.
- **Failure recovery** (retry and webhook probe) is a holder kind like any other. A failed partition is a recovery claim, so a retry cannot run beside a partition replay or catch-up (#5380).
- **Spilling live delivery to catch-up** is an acquisition, not a separate path. When the appended-events queue sheds an observer because its buffer is full, or because the observer falls behind live delivery (#5366), the resulting catch-up takes an observer-wide claim through the observer, under the same rules as any other observer-wide catch-up: claimed partitions are left out and their pending work waits. The queue removes the live subscription before asking, so live delivery is not a writer in between.
- **Abandoned webhook deliveries** are not owners, but a delivery fence blocks any grant until it expires, because a remote effect cannot be drained.

### How ownership moves

- **Acquire.** Every path that starts a writer acquires through the observer: a partition replay or catch-up request, a failure retry, an observer-wide catch-up or replay, and a spill from the appended-events queue. The observer records the claim (holder, new epoch, resume position) in memory in the same turn in which it decides, with no await between reading the current claim and replacing it, then persists it, then starts the job with an id derived from observer, partition and epoch. A crash after persisting re-issues the same job on activation.
- **Hand over.** A holder reports back with its epoch. A report with a stale epoch is ignored. The observer computes the successor in one turn: the next pending work, catch-up from the resume position if events may have arrived, or release to live delivery after a tail check that runs before the release with no await between the check and the release. Replay hands to catch-up, catch-up hands to live; no holder adopts another's job and no fence is reinstated.
- **Revoke.** Only an observer-wide replay, an operator action or removal revokes a claim. Revocation stops the holder's job and waits for the Cratis.Orleans drain to acknowledge; the claim stays until then. A job status of `Removing` or `Stopped` is not drained (#5378).
- **Recover.** The watchdog and routing never clear claims. For each claim they check whether its holder job is live or still draining. If the holder is gone, they re-issue the same kind of work under a new epoch from the resume position. A claim can only end through a hand over or a revocation, so it cannot be stranded and cannot be dropped (#5379).

### Which store is authoritative

The observer's state document is the single authority for ownership. Each claim transition is one conditional write of that document, guarded by an ownership revision in the same way the type-set control of decision 0011 is guarded; a write that loses the condition deactivates the activation that attempted it. Effects in other stores (resolving the failed-partition record, adjusting handled counts) are recorded as intent on the claim transition and applied idempotently after it, and re-applied on activation until they are recorded as done (#5383). The failed-partition store keeps failure history but no longer decides ownership.

### Invariants

1. **No loss.** Every event at or after a partition's resume position is delivered at least once by some owner before the partition is released to live delivery.
2. **One writer.** At no time do two writers deliver to the same partition; a writer whose epoch is stale delivers nothing further.
3. **No stranded hold.** After activation and one watchdog cycle, every claim has a holder that is live, draining or being re-issued.
4. **Monotone epochs.** A transition never restores an older epoch, so a stale report, start or save cannot take ownership back.

Delivery stays at least once: a crash between a delivery and its record may redeliver, as today.

### Relationship to decision 0004

- **Not a step toward lanes.** A lane in decision 0004 owns a hashed range of keys and accounts progress for it; a claim owns one partition and accounts nothing. The claim is not reused as lane ownership, and decision 0004 does not need this record.
- **Compatible concepts.** The claim's epoch plays the role of 0004's fenced lane attempt (accept only the current attempt, retire ambiguous attempts before replacing them), and pending work plays the role of its per-key repair obligations. Specs written for this record's invariants are meant to carry over to the lanes engine as regression cases.
- **Retired by finalization.** On a cluster that has finalized ordered lanes, the legacy catch-up, retry and replay job paths this record governs no longer deliver (0004 phase 7 removes them), so claims are ignored and then dropped. Until then the claims protect the engine that actually runs.
- **One floor, not three.** This record's enforcement floor must reuse whichever cluster-wide mechanism is decided for decision 0017 and for 0004's bridge and finalization, not add a third.
- **If 0004 is accepted first** with a near release, increments 4 to 7 below are reconsidered against it; increments 1 to 3 fix defects on the engine that runs today and stand either way.

### Mixed versions (co-authored with the rolling-upgrade work)

Principle: **client versions decide definition ownership by version; kernel versions decide nothing new, they only keep honoring fences written by either side.**

- **Additive state.** The claim and ownership revision are new, optional fields. Grain interfaces and grain payloads only grow within a major, and both contract gates apply. An older kernel must load a document carrying claims, and an older kernel's save must leave the claims untouched. TODO (rolling upgrades, #5169): confirm both for MongoDB and SQL against the oldest kernel the minor must coexist with.
- **Shadow before enforcement.** A kernel that knows claims writes them and keeps the derived replaying and catching-up sets, but enforces only behind the floor. Until the floor is reached, protection is today's behavior and never weaker: an older kernel that clears the derived sets during routing (#5379 on that kernel) cannot remove the claims, and the next newer activation reconciles from them.
- **Enforcement behind a floor, not a major.** Enforcement is enabled per event store by an explicit initialization that succeeds only when no silo older than the floor is in the cluster, and is never silently disabled afterwards, as in decision 0017. Job admission must be registered on every silo that can host the job types it concerns, which an older silo cannot do, so enforcing before the floor is not sound. TODO (rolling upgrades, #5169): confirm the floor mechanism is shared with #5169 rather than a second one.
- **Older silo activation.** Before the floor, an observer may activate on an older silo (#5384); that is accepted as today's behavior. After the floor no older silo exists. TODO (rolling upgrades, #5169): confirm #5384's placement fix does not need version-pinned placement for this record; if it does, enforcement waits for it.
- **Definition ownership.** Which client registration owns a reactor or reducer definition follows the explicit client version ([#5367](https://github.com/Cratis/Chronicle/issues/5367)), and moving an observer to another event sequence hands over cursor and failed partitions ([#5368](https://github.com/Cratis/Chronicle/issues/5368)) and deactivates the old activation ([#5337](https://github.com/Cratis/Chronicle/issues/5337), [#5338](https://github.com/Cratis/Chronicle/issues/5338)). Partition claims move with the cursor in that hand over; claims are never keyed by anything that differs between the two definitions.
- **The definition owner window.** [#5326](https://github.com/Cratis/Chronicle/issues/5326) accepts that an older kernel may drop the definition `Owner` during a rolling deploy, disabling its protection until a newer kernel stamps it again. This record accepts the same window for claims only before the floor, and only because it is no worse than today. After the floor it is not accepted. TODO (rolling upgrades, #5169): confirm or reject that window for #5326 on the same terms.

## Options considered

- **One durable claim per partition, drain before transfer, enforcement behind a floor (chosen).** Gives every writer one thing to check, closes #5378, #5379, #5380, #5382 and #5383 with one invariant set, and can ship in shadow first.
- **The four-state handoff per partition replay, as planned for rewinds.** Rejected. It added states on top of the existing holds rather than replacing them, it could interleave across awaits inside the handoff, its prepare step wrote two stores separately, and it changed what "the partition is fenced" means between versions. It also could not close the existing defects, which are where most of the review's findings came from.
- **Per-writer fences, as on the rewind branch.** Rejected. Each fence protects one writer from one other writer, adds a sixth notion of a held partition and a new interface method per fence, and still leaves the watchdog, routing, observer-wide jobs and retries unaware of it.
- **Storage-level conditional writes per delivery, like decision 0016.** Rejected as the ownership mechanism. It would put a fence on every sink and subscriber write, cannot fence a webhook's remote effect, and would still need someone to decide who holds the fence. The claim record itself is written conditionally; deliveries are not. Decision 0016 still fences materialization writes on its own terms.
- **Adopt decision 0004 instead.** Rejected as the answer for this engine, not as a direction. Ordered lanes would remove these writers rather than coordinate them, and would be the better end state. But 0004 is proposed, not accepted; it is planned as several dormant minor releases, an explicit cluster finalization and a major. Partition rewinds and the high-severity defects (#5379, #5380) cannot wait for all of that on the engine every cluster runs until then.
- **Wait for decision 0004 and fix only the defects.** Partly taken: increments 1 to 3 are exactly that and ship regardless. Rejected as the whole answer because #5380, #5382 and #5383 cannot be fixed without a shared notion of who holds a partition, and partition rewinds would stay blocked until a finalized lanes engine exists.
- **Typed recovery obligations as a second progress authority.** Rejected, as decision 0010 rejected it for this engine: claims decide who may write and leave progress where it is.
- **Do nothing and document the limits.** Rejected. Two of the defects lose events or let two writers deliver at once today, partition rewinds cannot be built safely, and each further delivery path would add its own guard.

## Default if unanswered

The defects that can be fixed on their own are fixed on main: #5381 (being fixed separately), #5378 and #5379. Partition rewinds stay blocked. No claim field is added and nothing changes in the mixed-version policy. The cost is that #5380, #5382 and #5383 stay open, because each needs a shared notion of ownership to fix without adding another ad-hoc hold.

## Timeline and scope

In scope: every writer listed in Context, the observer state document in every storage provider, job admission for observer job types, and the watchdog and routing. Out of scope: materialization fencing (decision 0016), the definition-ownership rules themselves (#5367, #5368), observer placement (#5384), and the rewind feature, which builds on this.

Increments, each shippable and reviewable on its own:

1. **Fix #5381 on main (separately owned).** Catch-up after a partition replay resumes no later than the first held-back event. Closes the event loss; prerequisite for the resume-position rule.
2. **Fix #5378 on main.** Treat `Removing` and `Stopped` as unfinished wherever the observer decides a job is gone. Closes premature "finished".
3. **Fix #5379 on main.** The watchdog recognizes partition catch-up jobs per held partition; routing keeps holds whose job is live or draining and re-issues catch-up for the rest instead of clearing them. No interface or state change. Closes the dropped hold.
4. **Introduce the claim in shadow.** Add the claim and ownership revision to the state document in every provider, write them on every transition alongside the existing sets, and report divergence. Closes nothing; proves the model under real traffic and settles the mixed-version storage questions.
5. **Deterministic job ids and admission by epoch.** Closes #5382.
6. **Single-document completion with idempotent side effects.** Closes #5383.
7. **Enforce behind the floor.** Every writer checks the claim, observer-wide jobs and partition claims exclude each other, retries become recovery claims, and the watchdog and routing reconcile from claims. Closes #5380 and the remaining hold races, and supersedes the guards from increments 2 and 3.
8. **Rebuild partition rewinds on claims** (#4334), with no fence of its own.

## Verification

**Done when:** for each storage provider, a crash-injection matrix covering every transition shows all four invariants hold; the seven listed issues are closed or explicitly re-scoped; and the floor refuses enforcement while an older silo is present.

**Verify by:**

- A spec per transition (acquire, report back, hand over, release, revoke, recover) in which each await is a crash point: crash before the in-memory change, after it but before the write, after the write but before the job start, and during the drain. Each case reactivates and asserts the invariants.
- The same transitions raced against each concurrent actor: a live append, a watchdog tick, routing, a retry, an observer-wide catch-up, a delayed job start, a job rehydrated on another silo, and a save from a kernel that does not know claims.
- A recording subscriber that fails the spec when two deliveries for one partition overlap, and an event ledger that fails it when an event at or after the resume position is never delivered.
- Mixed-version integration: a cluster with one silo on the previous minor and one on the new one, an observer moved between them mid-replay; claims survive and protection is never weaker than the previous minor.
- Integration with the real Cratis.Orleans job engine for drain and admission; a test double for either is not proof.
- **Throughput non-regression gate.** The ownership check per delivered batch, or a lease if one replaces it, must not cost catch-up or replay throughput. Run the clustering benchmark's `--catch-up-shape` and `--live-interleave` scenarios for a projection and a reactor at 100 event sources and 10,000 events, before enforcement (shadow) and after, on the same rig, several repetitions each, comparing medians. Proposed acceptance bound: at most 10% wall-time regression in every combination; the decider confirms the bound. A per-batch check that fails it is replaced by a lease that steps check locally, and the gate is re-run; this measurement settles the open question of check per batch versus lease.

## Consequences

Relationship to decision 0004: this record adds ownership rules to the engine 0004 proposes to replace, so part of the work is deliberately temporary. Its specs and invariants carry over; its claim storage and enforcement do not, and are removed once a cluster finalizes ordered lanes. If 0004 is accepted, its floor and this record's floor must be the same mechanism.

Easier: one place to ask who may write a partition; rewinds and faster replay can add holder kinds instead of fences; the watchdog becomes a reconciler that cannot drop work. Harder: every new delivery path must take a claim, the observer state document gains a conditional-write rule in every provider, and enforcement waits for a floor operators must reach. Partition replay requests made during an observer-wide catch-up wait for it to finish. Foreclosed: clearing holds as a recovery step, adding per-writer fences outside the claim, and starting a writer, including a spill from the appended-events queue, without acquiring through the observer.
