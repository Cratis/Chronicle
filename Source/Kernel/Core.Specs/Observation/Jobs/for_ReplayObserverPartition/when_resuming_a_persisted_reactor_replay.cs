// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_resuming_a_persisted_reactor_replay : given.a_partition_replay_job
{
    Exception _error;

    async Task Establish()
    {
        _request = _request with { ObserverType = ObserverType.Reactor };
        await _job.Start(_request);
        _replayStateServiceClient.ClearReceivedCalls();
    }

    async Task Because() => _error = await Catch.Exception(_job.ResumeForTesting);

    [Fact] void should_resume() => _error.ShouldBeNull();
    [Fact] async Task should_not_tell_the_reactor_again_that_its_partition_replay_began() => await _replayStateServiceClient.DidNotReceive().BeginReplayPartitionFor(Arg.Any<ObserverDetails>(), Arg.Any<Concepts.Keys.Key>());
}
