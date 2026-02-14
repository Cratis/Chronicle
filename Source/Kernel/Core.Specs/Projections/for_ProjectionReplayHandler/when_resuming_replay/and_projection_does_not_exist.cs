// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_resuming_replay;

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

    Task Because() => _handler.ResumeReplayFor(_observerDetails);

    [Fact] void should_not_resume_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().ResumeReplay(null!);
}
