// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_the_observer_is_quarantined_while_replaying : given.a_reactivated_quarantined_observer
{
    JobId _replayJob;

    void Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        _failedPartitionsState.AddFailedPartition("partition", 12UL);
        _stateStorage.State = _stateStorage.State with { IsReplaying = true, FailedPartitionCount = 1 };
        _replayJob = JobId.New();
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(new JobState
            {
                Id = _replayJob,
                Status = JobStatus.Stopped,
                Request = new ReplayObserverRequest(_observerKey, ObserverType.Projection, [])
            })));
        _observerHandledCountsStorage.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Retire();

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_quarantined_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_replay_flag() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_discard_failed_partitions_in_memory() => (await _observer.HasFailedPartitions()).ShouldBeFalse();
    [Fact] void should_discard_failed_partitions_in_storage() => _failedPartitionsStorage.State.HasFailedPartitions.ShouldBeFalse();
    [Fact] void should_reset_the_failed_partition_count() => _stateStorage.State.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] async Task should_not_read_the_tail_for_a_routing_pass() => await _eventSequence.DidNotReceive().GetTailSequenceNumber();
    [Fact] async Task should_not_start_a_replay_job() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_resume_the_paused_replay_job() => await _jobsManager.DidNotReceive().Resume(_replayJob);
    [Fact] async Task should_not_remove_the_handled_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
    [Fact] async Task should_clear_alerts_as_removed() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Retired));
    [Fact] async Task should_not_report_an_operator_clear() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Active));
}
