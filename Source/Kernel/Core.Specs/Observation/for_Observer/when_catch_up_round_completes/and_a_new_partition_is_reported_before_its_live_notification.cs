// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_new_partition_is_reported_before_its_live_notification : given.an_observer_behind_a_repair_barrier
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { PendingCatchUpRepairs = [] };
        _observer.SetSubscription(new ObserverSubscription(_observerId, _observerKey, [event_type], typeof(IObserverPartitionPageSubscriber), SiloAddress.Zero, null));
    }

    async Task Because()
    {
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "Q" });
        await _subscriber.OnNext("P", [Event(14UL)], new ObserverSubscriberContext(null));
        await _observer.ReportHandledEvents("P", new Dictionary<EventTypeId, EventCount> { [event_type.Id] = 1UL }, JobStepId.New(), 14UL);
        await _observer.Handle("P", [Event(14UL)]);
        await _observer.CaughtUp(JobId.NotSet, 16UL, 16UL);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_not_replay_the_already_reported_event() => _delivered.ShouldContainOnly((EventSequenceNumber)14UL);
    [Fact] void should_not_create_a_repair_for_the_reported_partition() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
    [Fact] void should_not_start_a_partition_job() => _jobsManager.DidNotReceive().Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_not_rewind_before_the_completed_walk() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
    [Fact] void should_not_report_a_spurious_skip() => _suspectedSkips.ShouldEqual(0);
}
