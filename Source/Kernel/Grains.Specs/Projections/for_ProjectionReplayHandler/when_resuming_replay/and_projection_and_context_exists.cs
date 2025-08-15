// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionReplayHandler.when_resuming_replay;

public class and_projection_and_context_exists : given.a_projection_replay_handler_with_projection
{
    ReplayContext _replayContext;

    void Establish()
    {
        _replayContext = new ReplayContext(_modelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_replayContext.ReadModel).Returns(_replayContext);
    }

    Task Because() => _handler.ResumeReplayFor(_observerDetails);

    [Fact] void should_resume_replay() => _projectionPipeline.Received(1).ResumeReplay(_replayContext);
}
