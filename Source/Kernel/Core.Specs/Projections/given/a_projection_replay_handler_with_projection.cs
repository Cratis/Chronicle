// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.given;

public class a_projection_replay_handler_with_projection : a_projection_replay_handler
{
    protected Engine.IProjection _projection;
    protected ReadModelDefinition _readModel;
    protected ReadModelType _readModelType = new("TheReadModelType", ReadModelGeneration.First);
    protected ReadModelContainerName _readModelName = "TheReadModel";

    void Establish()
    {
        var projectionDefinition = new ProjectionDefinition(
            ProjectionOwner.Client,
            Concepts.EventSequences.EventSequenceId.Log,
            _observerDetails.Key.ObserverId.Value,
            _readModelType.Identifier,
            true,
            true,
            new System.Text.Json.Nodes.JsonObject(),
            new Dictionary<Concepts.Events.EventType, FromDefinition>(),
            new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
            new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>(),
            null,
            DateTimeOffset.UtcNow,
            null,
            AutoMap.Enabled);

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
        _projectionFactory.Create(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            projectionDefinition,
            _readModel,
            Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(_projection);

        _projections.TryGet(
            _observerDetails.Key.EventStore,
            _observerDetails.Key.Namespace,
            (ProjectionId)_observerDetails.Key.ObserverId,
            out _).Returns(callInfo =>
            {
                callInfo[3] = _projection;
                return true;
            });

        var projectionGrain = Substitute.For<global::Cratis.Chronicle.Projections.IProjection>();
        projectionGrain.GetDefinition().Returns(projectionDefinition);
        _grainFactory.GetGrain<global::Cratis.Chronicle.Projections.IProjection>(Arg.Any<string>()).Returns(projectionGrain);

        var readModelDefinitions = Substitute.For<IReadModelDefinitionsStorage>();
        readModelDefinitions.Get(_readModelType.Identifier).Returns(_readModel);
        _eventStoreStorage.ReadModels.Returns(readModelDefinitions);

        var eventTypesStorage = Substitute.For<IEventTypesStorage>();
        eventTypesStorage.GetLatestForAllEventTypes().Returns([]);
        _eventStoreStorage.EventTypes.Returns(eventTypesStorage);
    }
}
