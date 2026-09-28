// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_establish_fails : given.a_projection_replay_handler_with_projection
{
    ICanHandleReplayForObserver.Error _error;

    void Establish() => _replayContexts.Establish(_readModelType, _readModelName)
        .Returns(Task.FromException<ReplayContext>(new InvalidOperationException("Establish failed")));

    async Task Because()
    {
        var result = await _handler.BeginReplayFor(_observerDetails);
        result.TryGetError(out _error);
    }

    [Fact] void should_report_the_establish_failure() => _error.ShouldEqual(ICanHandleReplayForObserver.Error.Unknown);
    [Fact] void should_not_begin_replay() => _projectionPipeline.DidNotReceiveWithAnyArgs().BeginReplay(null!);
}
