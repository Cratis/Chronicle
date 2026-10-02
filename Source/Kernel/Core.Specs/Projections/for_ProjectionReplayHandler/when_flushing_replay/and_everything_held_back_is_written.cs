// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Monads;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_flushing_replay;

public class and_everything_held_back_is_written : given.a_projection_replay_handler_with_projection
{
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish() => _projectionPipeline.EndBulk().Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([]));

    async Task Because() => _result = await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_write_what_the_pipeline_holds_back() => _projectionPipeline.Received(1).EndBulk();
    [Fact] void should_not_end_the_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().EndReplay(null!);
    [Fact] void should_not_evict_the_replay_context() => _replayContexts.DidNotReceiveWithAnyArgs().Evict(default!);
    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
}
