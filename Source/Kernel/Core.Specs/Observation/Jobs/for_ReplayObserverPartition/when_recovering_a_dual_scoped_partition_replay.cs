// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_recovering_a_dual_scoped_partition_replay : given.a_recovered_global_partition_replay
{
    bool _resumed;

    async Task Because()
    {
        IJob job = _job;
        _resumed = (await job.Resume()).IsSuccess;
    }

    [Fact] void should_refuse() => _resumed.ShouldBeFalse();
    [Fact] void should_fail_the_job_permanently() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_the_reason() => _stateStorage.State.StatusChanges[^1].ExceptionMessages.ShouldContain(GlobalProjectionReplay.RefusalReason);
    [Fact] void should_not_dispatch_to_silos() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
