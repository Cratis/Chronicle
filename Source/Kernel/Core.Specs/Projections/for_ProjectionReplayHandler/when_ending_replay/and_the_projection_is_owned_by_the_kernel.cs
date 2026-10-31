// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

public class and_the_projection_is_owned_by_the_kernel : given.a_kernel_owned_projection_replay_handler
{
    ReplayContext _replayContext;

    void Establish()
    {
        _replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(_replayContext);
    }

    Task Because() => _handler.EndReplayFor(_observerDetails);

    [Fact] void should_signal_replayed_to_replay_manager() => _readModelReplayManager.Received(1).Replayed(_observerDetails.Key.ObserverId, _replayContext);
    [Fact] void should_discard_the_superseded_generations() => _readModelReplayManager.Received(1).ApplyRetentionPolicy(0);
}
