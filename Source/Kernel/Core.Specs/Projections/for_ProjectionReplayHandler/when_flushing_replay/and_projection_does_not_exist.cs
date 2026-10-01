// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_flushing_replay;

public class and_projection_does_not_exist : given.a_projection_replay_handler
{
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish()
    {
        _projections.TryGet(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            (ProjectionId)_observerDetails.Key.ObserverId,
            out _).Returns(false);
    }

    async Task Because() => _result = await _handler.FlushReplayFor(_observerDetails);

    [Fact] void should_not_write_anything() => _projectionPipeline.DidNotReceive().EndBulk();
    [Fact] void should_succeed() => _result.TryGetError(out _).ShouldBeFalse();
}
