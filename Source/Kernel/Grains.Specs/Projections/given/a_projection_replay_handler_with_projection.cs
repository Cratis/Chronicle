// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler_with_projection : a_projection_replay_handler
{
    protected Chronicle.Projections.IProjection _projection;
    protected ReadModelDefinition _model;
    protected ReadModelName _modelName = "TheModel";

    void Establish()
    {
        _projection = Substitute.For<Chronicle.Projections.IProjection>();
        _model = new(_modelName, null!);
        _projection.ReadModel.Returns(_model);

        _projections.TryGet(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            (ProjectionId)_observerDetails.Key.ObserverId,
            out _).Returns(callInfo =>
            {
                callInfo[3] = _projection;
                return true;
            });
    }
}
