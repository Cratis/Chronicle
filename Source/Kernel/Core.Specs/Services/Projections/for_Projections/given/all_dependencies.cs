// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

/// <summary>
/// Base context that wires up all dependencies of the Projections gRPC service.
/// </summary>
public class all_dependencies : Specification
{
    protected static readonly EventStoreName EventStore = "test-store";
    protected static readonly EventStoreNamespaceName EventStoreNamespace = "test-namespace";
    protected const string ReadModelName = "test-read-model";

    protected IGrainFactory _grainFactory;
    protected IExpandoObjectConverter _expandoObjectConverter;
    protected ILanguageService _languageService;
    protected IReadModelsCompliance _readModelsCompliance;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IReadModelDefinitionsStorage _readModelDefinitionsStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IEventCursor _eventCursor;
    protected IProjection _projectionGrain;
    protected ReadModelDefinition _readModelDefinition;
    protected ProjectionDefinition _projectionDefinition;
    protected Contracts.Projections.IProjections _service;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _languageService = Substitute.For<ILanguageService>();
        _readModelsCompliance = Substitute.For<IReadModelsCompliance>();

        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _readModelDefinitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _eventCursor = Substitute.For<IEventCursor>();
        _projectionGrain = Substitute.For<IProjection>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        _eventStoreStorage.ReadModels.Returns(_readModelDefinitionsStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);

        _readModelDefinitionsStorage.GetAll().Returns([]);
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([]);

        // No events are collected for preview - the projection grain itself is mocked to return the
        // projected instances directly, so what the cursor yields is irrelevant to the specs.
        _eventCursor.MoveNext().Returns(false);
        _eventSequenceStorage.GetEventsWithLimit(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<int>(),
            Arg.Any<EventSourceId?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<IEnumerable<EventType>?>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<CancellationToken>())
            .Returns(_eventCursor);

        _projectionGrain.GetEventTypes().Returns([]);
        _grainFactory.GetGrain<IProjection>(Arg.Any<string>()).Returns(_projectionGrain);

        _readModelDefinition = new ReadModelDefinition(
            ReadModelName,
            ReadModelName,
            "Test Read Model",
            ReadModelOwner.Server,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, new JsonSchema { Type = JsonObjectType.Object, Title = ReadModelName } } },
            []);

        _projectionDefinition = new ProjectionDefinition(
            ProjectionOwner.Server,
            EventSequenceId.Log,
            "preview",
            ReadModelName,
            true,
            true,
            new JsonObject(),
            new Dictionary<EventType, FromDefinition>(),
            new Dictionary<EventType, JoinDefinition>(),
            new Dictionary<PropertyPath, ChildrenDefinition>(),
            [],
            new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
            new Dictionary<EventType, RemovedWithDefinition>(),
            new Dictionary<EventType, RemovedWithJoinDefinition>());

        // Converts an ExpandoObject to JSON by copying every property across as a plain string, which is
        // enough for the specs to inspect the value that reached the client without a real schema-driven
        // converter.
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call =>
            {
                var json = new JsonObject();
                foreach (var (key, value) in (IDictionary<string, object?>)call.Arg<ExpandoObject>())
                {
                    json[key] = value?.ToString();
                }

                return json;
            });

        SetReadModels(_readModelDefinition);
        SetCompiledDefinition(_projectionDefinition);

        var services = new ServiceCollection();
        services.AddSingleton(_storage);
        _service = new Projections(
            _grainFactory,
            _expandoObjectConverter,
            _languageService,
            services.BuildServiceProvider(),
            _readModelsCompliance);
    }

    /// <summary>
    /// Configures the read models the compiler and Preview see as already registered.
    /// </summary>
    /// <param name="readModels">The read model definitions to make available.</param>
    protected void SetReadModels(params ReadModelDefinition[] readModels) =>
        _readModelDefinitionsStorage.GetAll().Returns(readModels.AsEnumerable());

    /// <summary>
    /// Configures the definition the mocked <see cref="ILanguageService"/> compiles the declaration into.
    /// </summary>
    /// <param name="definition">The definition the compile step should produce.</param>
    protected void SetCompiledDefinition(ProjectionDefinition definition) =>
        _languageService.Compile(
            Arg.Any<string>(),
            Arg.Any<ProjectionOwner>(),
            Arg.Any<IEnumerable<ReadModelDefinition>>(),
            Arg.Any<IEnumerable<EventTypeSchema>>())
            .Returns(Result.Success<ProjectionDefinition, CompilerErrors>(definition));

    /// <summary>
    /// Configures the instances the mocked projection grain projects the (empty, mocked-away) events into.
    /// </summary>
    /// <param name="instances">The projected instances <see cref="IProjection.Process"/> should return.</param>
    protected void SetProjectedInstances(params ExpandoObject[] instances) =>
        _projectionGrain.Process(Arg.Any<EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(instances.AsEnumerable());
}
