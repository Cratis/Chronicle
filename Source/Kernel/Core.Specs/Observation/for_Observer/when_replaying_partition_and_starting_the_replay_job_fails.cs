// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_and_starting_the_replay_job_fails : given.an_observer
{
    static Key _partition;
    Exception? _error;

    void Establish()
    {
        _partition = "some-partition";
        _jobsManager
            .Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
            .Returns(Task.FromException<Result<JobId, StartJobError>>(new InvalidOperationException("Starting the replay job failed")));
    }

    async Task Because() => _error = await Cratis.Specifications.Catch.Exception(() => _observer.ReplayPartition(_partition));

    [Fact] void should_fail_the_replay_request() => _error.ShouldNotBeNull();
    [Fact] void should_not_leave_the_partition_replaying() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
}
