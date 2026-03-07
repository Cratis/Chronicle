// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_projection_does_not_exist : given.a_projection_replay_handler
{
    void Establish()
    {
        _projections.TryGet(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            (ProjectionId)_observerDetails.Key.ObserverId,
            out _).Returns(false);
    }

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_not_establish_context() => _replayContexts.DidNotReceiveWithAnyArgs().Establish(null!, null!);
    [Fact] void should_not_begin_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().BeginReplay(null!);
}
