// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_starting_a_dual_scoped_partition_replay : given.a_recovered_global_partition_replay
{
    bool _started;

    void Establish()
    {
        _stateStorage.State.Status = JobStatus.None;
        _job.PrepareSynchronouslyForTesting = true;
    }

    async Task Because()
    {
        _started = (await _job.Start(_request)).IsSuccess;
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _started.ShouldBeFalse();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_touch_the_sink() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_the_partition_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedPartitionReplay(_request.Key, _request.HeldBackFrom);
    [Fact] void should_only_release_the_partition() => _observer.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IObserver.ReleaseRefusedPartitionReplay));
}
