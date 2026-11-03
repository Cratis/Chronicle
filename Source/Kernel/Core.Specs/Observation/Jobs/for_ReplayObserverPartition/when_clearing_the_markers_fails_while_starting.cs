// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_clearing_the_markers_fails_while_starting : given.a_partition_replay_job
{
    Exception _error;

    async Task Establish()
    {
        await _job.Start(_request);
        _partitionReplayMarkers.ClearReceivedCalls();
        _partitionReplayMarkers
            .ClearFor(Arg.Any<ObserverDetails>(), Arg.Any<Key>())
            .Returns(Task.FromException(new InvalidOperationException("sink unavailable")));
    }

    async Task Because() => _error = await Catch.Exception(_job.StartPreparedStepsForTesting);

    [Fact] void should_not_swallow_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
