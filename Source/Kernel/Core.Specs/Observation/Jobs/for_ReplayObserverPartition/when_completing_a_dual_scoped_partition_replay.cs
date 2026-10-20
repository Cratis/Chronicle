// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_completing_a_dual_scoped_partition_replay : given.a_recovered_global_partition_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(_job.CompleteForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_not_promote_a_sink() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_the_observer_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedPartitionReplay(_request.Key);
    [Fact] void should_not_claim_a_successful_replay() => _observer.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IObserver.ReleaseRefusedPartitionReplay));
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
}
