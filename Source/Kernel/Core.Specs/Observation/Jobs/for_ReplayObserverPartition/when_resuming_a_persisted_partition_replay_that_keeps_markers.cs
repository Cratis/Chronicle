// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_resuming_a_persisted_partition_replay_that_keeps_markers : given.a_partition_replay_job
{
    async Task Establish()
    {
        await _job.Start(_request);
        _partitionReplayMarkers.ClearReceivedCalls();
        _replayStateServiceClient.ClearReceivedCalls();
    }

    async Task Because() => await _job.ResumeForTesting();

    [Fact] async Task should_clear_the_markers_of_the_replayed_partition_again() => await _partitionReplayMarkers.Received(1).ClearFor(new ObserverDetails(_request.ObserverKey, _request.ObserverType), _request.Key);
    [Fact] async Task should_not_begin_the_partition_replay_again() => await _replayStateServiceClient.DidNotReceive().BeginReplayPartitionFor(Arg.Any<ObserverDetails>(), Arg.Any<Key>());
}
