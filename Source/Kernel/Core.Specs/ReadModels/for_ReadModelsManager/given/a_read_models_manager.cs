// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.given;

public class a_read_models_manager : Specification
{
    protected TestKitSilo _silo = new();
    protected ReadModelsManager _manager;
    protected IReadModel _readModelGrain;
    protected IProjectionsManager _projectionsManagerGrain;
    protected INamespaces _namespacesGrain;
    protected IProjectionPipelineManager _projectionPipelines;

    protected static readonly EventStoreName _eventStore = "some-event-store";

    async Task Establish()
    {
        _readModelGrain = Substitute.For<IReadModel>();
        _silo.AddProbe(_ => _readModelGrain);

        _projectionsManagerGrain = Substitute.For<IProjectionsManager>();
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([]);
        _silo.AddProbe(_ => _projectionsManagerGrain);

        _namespacesGrain = Substitute.For<INamespaces>();
        _namespacesGrain.GetAll().Returns([(EventStoreNamespaceName)"default"]);
        _silo.AddProbe(_ => _namespacesGrain);

        _projectionPipelines = Substitute.For<IProjectionPipelineManager>();
        _silo.AddService(_projectionPipelines);

        _manager = await _silo.CreateGrainAsync<ReadModelsManager>(_eventStore.Value);
    }

    protected static ProjectionDefinition ProjectionTargeting(ProjectionId identifier, ReadModelIdentifier readModel) => new(
        ProjectionOwner.Client,
        Concepts.EventSequences.EventSequenceId.Log,
        identifier,
        readModel,
        true,
        true,
        new System.Text.Json.Nodes.JsonObject(),
        new Dictionary<Concepts.Events.EventType, FromDefinition>(),
        new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
        new Dictionary<Properties.PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<Properties.PropertyPath, string>(), false),
        new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
        new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>());

    protected static ReadModelDefinition DefinitionFor(ReadModelIdentifier identifier, ReadModelDisplayName displayName) => new(
        identifier,
        identifier.Value,
        displayName,
        ReadModelOwner.None,
        ReadModelSource.Unknown,
        ReadModelObserverType.NotSet,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(),
        []);
}
