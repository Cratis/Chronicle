// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_abandoning_replay;

public class and_the_context_does_not_exist : given.a_projection_replay_handler_with_projection
{
    void Establish() => _replayContexts.TryGet(_readModelType.Identifier).Returns(GetContextError.NotFound);

    Task Because() => _handler.AbandonReplayFor(_observerDetails);

    [Fact] void should_leave_replay_on_this_silo() => _projectionPipeline.Received(1).LeaveReplay();
    [Fact] void should_flush_what_is_held_back_before_leaving() => _projectionPipeline.Received(1).EndBulk();
    [Fact] void should_not_evict_a_context() => _replayContexts.DidNotReceiveWithAnyArgs().Evict(default!);
}
