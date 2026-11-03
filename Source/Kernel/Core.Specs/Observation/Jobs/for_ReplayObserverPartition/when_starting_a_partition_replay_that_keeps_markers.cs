// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_starting_a_partition_replay_that_keeps_markers : given.a_partition_replay_job
{
    readonly List<string> _calls = [];

    async Task Establish()
    {
        await _job.Start(_request);
        _partitionReplayMarkers.ClearReceivedCalls();
        _replayStateServiceClient
            .BeginReplayPartitionFor(Arg.Any<ObserverDetails>(), Arg.Any<Key>())
            .Returns(_ =>
            {
                _calls.Add("begin");
                return Task.CompletedTask;
            });
        _partitionReplayMarkers
            .ClearFor(Arg.Any<ObserverDetails>(), Arg.Any<Key>())
            .Returns(_ =>
            {
                _calls.Add("clear");
                return Task.CompletedTask;
            });
    }

    async Task Because() => await _job.StartPreparedStepsForTesting();

    [Fact] async Task should_clear_the_markers_of_the_replayed_partition() => await _partitionReplayMarkers.Received(1).ClearFor(new ObserverDetails(_request.ObserverKey, _request.ObserverType), _request.Key);
    [Fact] void should_clear_after_the_partition_replay_began() => _calls.ShouldEqual(["begin", "clear"]);
}
