// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionReplayHandler.when_ending_replay;

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

    Task Because() => _handler.EndReplayFor(_observerDetails);

    [Fact] void should_not_end_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().EndReplay(null!);
}
