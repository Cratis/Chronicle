// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_resuming_a_persisted_global_replay : given.a_partition_replay_job
{
    Exception _error;
    ObserverDetails _details;

    async Task Establish()
    {
        await _job.Start(_request);
        _details = new(_request.ObserverKey, ObserverType.Projection);
        _replayStateServiceClient.ClearReceivedCalls();
        _replayStateServiceClient.BeginReplayPartitionFor(_details, _request.Key)
            .Returns(_ => Task.FromException(new GlobalProjectionReplayNotSupported()));
    }

    async Task Because() => _error = await Catch.Exception(_job.ResumeForTesting);

    [Fact] void should_propagate_the_refusal_before_resuming_steps() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] async Task should_check_the_persisted_projection_scope() => await _replayStateServiceClient.Received(1).BeginReplayPartitionFor(_details, _request.Key);
}
