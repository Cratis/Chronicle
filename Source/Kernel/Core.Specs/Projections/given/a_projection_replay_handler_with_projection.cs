// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler_with_projection : a_projection_replay_handler
{
    protected Engine.IProjection _projection;
    protected ProjectionDefinition _projectionDefinition;
    protected ReadModelDefinition _readModel;
    protected ReadModelType _readModelType = new("TheReadModelType", ReadModelGeneration.First);
    protected ReadModelContainerName _readModelName = "TheReadModel";

    void Establish()
    {
        _projection = Substitute.For<Engine.IProjection>();
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

        _projectionDefinition = new ProjectionDefinition(
            ProjectionOwner.Client,
            EventSequenceId.Log,
            (ProjectionId)_observerDetails.Key.ObserverId,
            _readModelType.Identifier,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow,
            null,
            AutoMap.Enabled);

        _projectionGrain.GetDefinition().Returns(Task.FromResult(_projectionDefinition));
        _readModelDefinitions.Get(_readModelType.Identifier).Returns(Task.FromResult(_readModel));
        _projectionFactory.Create(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            _projectionDefinition,
            _readModel,
            Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(Task.FromResult(_projection));

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
