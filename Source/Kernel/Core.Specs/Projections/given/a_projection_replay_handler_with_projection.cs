// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using NJsonSchema;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler_with_projection : a_projection_replay_handler
{
    protected Chronicle.Projections.IProjection _projection;
    protected ReadModelDefinition _readModel;
    protected ReadModelType _readModelType = new("TheReadModelType", ReadModelGeneration.First);
    protected ReadModelContainerName _readModelName = "TheReadModel";

    void Establish()
    {
        _projection = Substitute.For<Chronicle.Projections.IProjection>();
        _readModel = new ReadModelDefinition(
            _readModelType.Identifier,
            _readModelName,
            new ReadModelDisplayName(_readModelName.Value),
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema() } },
            []);
        _projection.ReadModel.Returns(_readModel);

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
