// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_fencing_partition_replay;

public class and_full_replay_is_suspended_in_quarantine : given.an_observer_with_partition_replay_jobs
{
    async Task Establish()
    {
        _observer.SetSubscription(new(_observerId, _observerKey, [EventType.Unknown], typeof(ObserverSubscriber), SiloAddress.Zero, null));
        await _observer.TransitionTo<Routing>();
        await _observer.TransitionTo<QuarantinedObserver>();
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        _stateStorage.State.ReplayingPartitions.Add(_partition);
        _jobStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionReplayed(_partition, 42UL, 48UL, [], _firstJob);
        await RunRetryTimer();
    }

    [Fact] void should_keep_the_full_replay_pending() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_retain_the_partition_fence() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_retain_the_recorded_completion() => _stateStorage.State.PartitionReplayCompletions!.Single().HandedOff.ShouldBeFalse();
    [Fact] void should_not_reconcile_partition_jobs() => _jobStorage.DidNotReceive().GetJobs<IReplayObserverPartition, JobState>();
    [Fact] void should_not_start_catchup() => CheckDidNotStartCatchupJob();
}
