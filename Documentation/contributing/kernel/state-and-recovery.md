---
title: State ownership and recovery
description: Why Chronicle stores event, observer and read-model state separately, what activation reconstructs, and where interrupted processing can repeat work.
---

An event is stored and its projection has changed a read model. Why might Chronicle process that event again after restart?

The event log, observer progress and read model record different facts. Saving one does not save the others. Recovery uses those records to decide what work is still owed; it cannot infer a completed subscriber call just because the read model looks current.

This explanation follows one hypothetical order through ordinary live processing: order `order-17` has quantity 4, and event 42 adds 2 units. The projection uses the event source as its key, so this order's events update its own read-model instance. There is one partition and no open bulk or replay window. The [append-to-delivery explanation](./append-to-delivery.md) covers the queue and live-to-catch-up handoff in more detail.

## Different stored records answer different questions

For this order, “event 42 exists,” “the observer handled event 42” and “the model has quantity 6” are three separate statements.

| Stored record | Owner and purpose | What it does not establish |
| --- | --- | --- |
| Events in the sequence | The event-sequence storage holds the events that can be read again. The `EventSequence` grain coordinates appends. | That any subscriber has finished processing them. |
| Event-sequence state snapshot | The event-sequence grain stores allocator and per-event-type tail state as a warm-start snapshot. Activation reconciles it with the stored events. | That a snapshot contains the latest append position. |
| Observer definition and processing state | Observer storage holds the definition; the `Observer` grain maintains progress and partition markers through its state provider. | That a read-model write and the observer's saved progress committed together. |
| Failed partitions | A separate failed-partition store retains failure and retry information used by the observer. | That restarting has removed the failure or made a retry eligible. |
| Projection definition | The `Projection` grain persists the definition used to construct processing behavior. | The observer's progress or the contents of every read model. |
| Read model and its watermark | The sink stores the model and, where supported, its last-handled-event watermark. | That the observer has recorded the subscriber's outcome. |

Read models and progress are derived from processing events, but that does not make their stored state unimportant. Recovery needs to distinguish work already acknowledged from work whose outcome it cannot establish. It also needs to retain intentional stops, such as quarantine.

The [event-sequence state provider](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/EventSequences/EventSequencesStorageProvider.cs) and [observer state provider](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/ObserverStateGrainStorageProvider.cs) show these different reconstruction paths. The observer provider loads partition failures separately from the observer's state; they are not a single record.

## Follow event 42 through normal processing

Assume the observer's saved last-handled position is 41 and the order's model still has quantity 4.

1. **Store the event.** The event-sequence grain completes the append before dispatching event 42 toward observers. Its warm-start snapshot is separate from event storage and may be written periodically. Queue admission is another boundary: if dispatch cannot keep the event on the live path, stored events remain the basis for catch-up.
2. **Record the subscriber call as in flight.** Before invoking the subscriber for this partition, the observer adds the partition to its in-flight set and persists that state. The marker means a call may need recovery if processing is interrupted. It does not say whether the subscriber changed anything.
3. **Read, compute and save the model.** The projection subscriber passes event 42 through its pipeline. The pipeline resolves the key, obtains the model's current state, computes the changes and reaches the save step. For this example, the intended change is quantity 4 to quantity 6.
4. **Return a processing outcome.** The save step calls the sink. Pipeline completion feeds the subscriber's success accounting; a failed step or later failure can instead produce a failed outcome. A subscriber response is not the same thing as the event's append response.
5. **Record the observer outcome.** The observer interprets the response, updates its handled/next positions, removes the in-flight marker and persists the outcome. Failure information can also need persistence. These calls do not make the earlier sink write and the later observer state write one transaction.

The order matters because there is work between the sink call and the saved observer outcome. There can also be failure after the projection pipeline has run: the subscriber notifies about saved model changes, and that notification can fail. A failed response therefore does not by itself prove that the model is unchanged.

The implementation boundaries are in [`HandleEventsForPartition`](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/Observer.Handling.cs), the [projection subscriber](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Projections/ProjectionObserverSubscriber.cs), and the pipeline's [save step](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Projections/Engine/Pipelines/Steps/SaveChanges.cs). The sequence position identifies event 42; observer progress records handling; the model watermark belongs to a sink write. None is a substitute for the other two.

## What activation can reconstruct

Suppose the event-sequence snapshot predates event 42. The state provider does not simply reuse its saved allocator position: it reads the actual stored tail and chooses the next sequence number from that tail. It also rebuilds stale or missing per-event-type tails from stored events. The snapshot is an optimization, not the final authority for deciding whether event 42 exists.

The observer takes a different path. Its state provider reloads saved progress and partition sets, then combines them with separately stored failures. The [observer's activation and subscription recovery](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/Observer.cs) use that information to resume work. Recovery depends on subscription and eligibility checks; activating a grain is not evidence that all pending work has completed.

The projection subscriber reloads the stored projection definition and constructs its pipeline again. That reconstructs the processing behavior, not the read-model contents. Activation does not automatically rebuild every model from the beginning, nor does it replace the observer's saved progress with whatever watermark a model happens to contain.

A persisted quarantine is different again: the observer resumes the quarantined state rather than treating a new activation as permission to continue.

## Stop after the model write, before the observer checkpoint

Now interrupt the example after the sink has saved quantity 6, but before the observer has saved a successful outcome for event 42. Persisted observer progress can still precede event 42, with the partition marked in flight.

On recovery, Chronicle cannot assume that call failed without changing the model. It also cannot assume it succeeded and skip work solely because a call was started. [Interrupted-call recovery](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/States/CatchingUpInFlight.cs) can start partition catch-up from the event after the saved handled position. Event 42 may therefore reach the subscriber again.

Repeating “add 2” against quantity 6 would produce 8, not 6. This is why a read-model watermark can matter independently of observer progress. For eligible existing instances of an event-source-keyed projection, when watermark-guarded writes are enabled, the pipeline asks the sink to apply a change only when the incoming sequence number advances the stored watermark. A sink implementing that mode can reject a repeated write for event 42 even though the observer needs to deliver it again.

That qualification is important. Eligibility, configuration and sink support all matter. The [sink contract](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Storage/Sinks/ISink.cs) allows implementations that do not honor the optional write mode; the [MongoDB sink](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Storage.MongoDB/Sinks/Sink.cs) implements watermark-aware paths. This is not a guarantee that every sink, projection shape or external side effect is deduplicated.

The example excludes an open bulk window for the same reason: a sink call in bulk mode may buffer a write. Bulk flushing and job checkpoints have additional boundaries, so returning from a sink method must not universally be described as a confirmed stored write.

## Known failure is not the same as an unknown outcome

If processing event 42 reports a partition failure, the observer records failure information and holds that partition out of ordinary live delivery. Recovery then owes work for the failed partition. It is not equivalent to advancing past the event and forgetting it.

Retry is conditional. The observer checks subscription, partition state, retry budget and quarantine before admitting recovery work. Retry scheduling does not promise a particular completion time. Quarantine stops further retry work; restarting does not clear that decision. The [failure handling](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/Observer.Failing.cs) and [partition recovery](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/Observation/Observer.PartitionRecovery.cs) code separate those eligibility decisions from the fact that a failure exists.

Starting recovery is itself a boundary. If a catch-up job is explicitly not admitted, interrupted-call recovery keeps the partition in flight rather than pretending a job owns it. An ambiguous start cannot safely be treated as that same refusal: the code retains a catching-up marker and can quarantine the observer because it cannot establish whether work was admitted. A recovery marker is therefore not a promise that a job is running or will eventually finish.

Uncertainty can also arise earlier, while appending event 42. Chronicle can try to read back the attempted stored event and match it to the append. A failed or timed-out probe cannot establish that no event was stored. The [append settlement code](https://github.com/Cratis/Chronicle/blob/f4921451764fa7fbef975783bfef04374432571a/Source/Kernel/Core/EventSequences/EventSequence.AppendSettlement.cs) preserves that distinction through an unknown-outcome path. Neither a missing acknowledgement nor an unknown result means “rolled back,” and this mechanism is not a general promise that arbitrary client retries are deduplicated.

## Keep the boundaries separate

For order `order-17`, recovery needs more information than the model's quantity:

- The stored event determines what can be read again.
- Saved observer progress and partition markers determine what processing may still be owed.
- Failed-partition records and quarantine determine whether recovery may proceed.
- The sink's model and watermark determine the stored projection result and any supported repeated-write guard.

These mechanisms explain how Chronicle can resume interrupted processing without assuming that all stored records changed together. They do not establish exactly-once processing, cross-store atomicity, every backend's durability guarantees or automatic recovery from data loss. Replay, bulk-window recovery, delivery fences and arbitrary subscriber side effects require their own explanations.

The implementation links in this page identify the source used for this explanation. They do not imply that every behavior is present in every checkout or released version.
