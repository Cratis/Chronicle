// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_projection_does_not_exist : given.a_projection_replay_handler
{
    ICanHandleReplayForObserver.Error _error;

    async Task Because()
    {
        var result = await _handler.BeginReplayFor(_observerDetails);
        result.TryGetError(out _error);
    }

    [Fact] void should_report_the_projection_as_not_found() => _error.ShouldEqual(ICanHandleReplayForObserver.Error.ProjectionNotFound);
    [Fact] void should_not_establish_context() => _replayContexts.DidNotReceiveWithAnyArgs().Establish(null!, null!);
    [Fact] void should_not_begin_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().BeginReplay(null!);
}
