// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_checking_if_a_global_replay_can_resume : given.a_recovered_global_partition_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(_job.CanResumeForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_the_reason() => _stateStorage.State.StatusChanges[^1].ExceptionMessages.ShouldContain(GlobalProjectionReplay.RefusalReason);
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedPartitionReplay(_request.Key);
    [Fact] void should_not_dispatch_to_silos() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_claim_a_successful_replay() => _observer.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IObserver.ReleaseRefusedPartitionReplay));
}
