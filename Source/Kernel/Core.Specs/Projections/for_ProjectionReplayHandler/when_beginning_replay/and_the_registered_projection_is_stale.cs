// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_the_registered_projection_is_stale : given.a_projection_replay_handler_with_projection
{
    Engine.IProjection _current;

    void Establish()
    {
        _current = Substitute.For<Engine.IProjection>();
        _current.ReadModel.Returns(_readModel);
        _projections.GetCurrent(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _observerDetails.Key.ObserverId).Returns(_current);
        _replayContexts.Establish(_readModelType, _readModelName).Returns(new ReplayContext(_readModelType, _readModelName, "revert", DateTimeOffset.UtcNow));
    }

    Task Because() => _handler.BeginReplayFor(_observerDetails);

    [Fact] async Task should_build_with_the_validated_projection() => await _projectionPipelineManager.Received(1).GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _current);
    [Fact] async Task should_not_build_with_the_stale_registered_projection() => await _projectionPipelineManager.DidNotReceive().GetFor(_observerDetails.Key.EventStore, _observerDetails.Key.Namespace, _projection);
}
