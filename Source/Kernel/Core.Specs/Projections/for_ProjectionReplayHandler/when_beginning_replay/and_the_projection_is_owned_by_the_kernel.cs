// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_the_projection_is_owned_by_the_kernel : given.a_kernel_owned_projection_replay_handler
{
    void Establish()
    {
        var replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.Establish(_readModelType, _readModelName).Returns(replayContext);
    }

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] void should_not_keep_any_earlier_generation() => _readModelReplayManager.Received(1).ApplyRetentionPolicy(0);
}
