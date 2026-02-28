// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_projection_exists : given.a_projection_replay_handler_with_projection
{
    ReplayContext _replayContext;

    void Establish()
    {
        _replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.Establish(_readModelType, _readModelName).Returns(_replayContext);
    }

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_begin_replay() => _replayContexts.Received(1).Establish(_readModelType, _readModelName);
}
