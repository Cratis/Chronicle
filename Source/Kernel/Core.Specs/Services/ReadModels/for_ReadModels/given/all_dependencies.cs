// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.given;

/// <summary>
/// Base context that wires up all dependencies of the ReadModels gRPC service.
/// </summary>
public class all_dependencies : Specification
{
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected ISinks _sinks;
    protected ISink _sink;
    protected IReadModel _readModel;
    protected ReadModelDefinition _readModelDefinition;
    protected IJsonComplianceManager _complianceManager;
    protected Contracts.ReadModels.IReadModels _service;

    protected IClusterClient _clusterClient;

    void Establish()
    {
        _clusterClient = Substitute.For<IClusterClient, IKeyedServiceProvider>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();

        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _sinks = Substitute.For<ISinks>();
        _sink = Substitute.For<ISink>();
        _readModel = Substitute.For<IReadModel>();

        _readModelDefinition = new ReadModelDefinition(
            "test-read-model",
            "test-container",
            "Test Read Model",
            ReadModelOwner.None,
            ReadModelSource.Unknown,
            ReadModelObserverType.Projection,
            "test-observer",
            new SinkDefinition(SinkConfigurationId.None, SinkTypeId.None),
            new Dictionary<ReadModelGeneration, JsonSchema> { { (ReadModelGeneration)1, new JsonSchema() } },
            []);

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Sinks.Returns(_sinks);
        _sinks.GetFor(Arg.Any<ReadModelDefinition>()).Returns(_sink);

        _readModel.GetDefinition().Returns(_readModelDefinition);
        _grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(_readModel);

        var expandoObjectConverter = Substitute.For<IExpandoObjectConverter>();
        _complianceManager = Substitute.For<IJsonComplianceManager>();

        _service = new ReadModels(
            _clusterClient,
            _grainFactory,
            _storage,
            expandoObjectConverter,
            _complianceManager,
            new JsonSerializerOptions());
    }
}
