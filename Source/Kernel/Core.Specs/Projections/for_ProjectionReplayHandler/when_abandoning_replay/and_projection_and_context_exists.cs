// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_abandoning_replay;

public class and_projection_and_context_exists : given.a_projection_replay_handler_with_projection
{
    ReplayContext _replayContext;

    void Establish()
    {
        _replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(_replayContext);
    }

    Task Because() => _handler.AbandonReplayFor(_observerDetails);

    [Fact] void should_flush_what_is_held_back() => _projectionPipeline.Received(1).EndBulk();
    [Fact] void should_leave_replay() => _projectionPipeline.Received(1).LeaveReplay();
    [Fact] void should_not_promote_the_rebuilt_read_model() => _projectionPipeline.DidNotReceiveWithAnyArgs().EndReplay(null!);
    [Fact] void should_not_signal_replayed_to_replay_manager() => _readModelReplayManager.DidNotReceiveWithAnyArgs().Replayed(default!, null!);
    [Fact] void should_evict_the_replay_context() => _replayContexts.Received(1).Evict(_readModelType.Identifier);
}
