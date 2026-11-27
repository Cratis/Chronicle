// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.Sinks;
using Definitions = Cratis.Chronicle.Projections.for_ProjectionsManager.given.a_projections_manager_grain;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance.given;

public class a_provenance_inventory : Specification
{
    protected static readonly EventStoreName Store = "store";
    protected readonly Dictionary<(EventStoreNamespaceName Namespace, SinkConfigurationId Configuration), ISinkProvenanceStorage> _markers = new();
    protected IStorage _storage;
    protected IEventStoreStorage _eventStore;
    protected bool _inventoryComplete;
    protected ReadModelSinkProvenance _provenance;
    protected ReadModelDefinition _model;
    protected ProjectionDefinition _projection;
    protected ReadModelDefinition[] _models;
    protected ProjectionDefinition[] _projections;
    protected ProjectionDefinitionHistoryEntry[] _history = [];
    protected NamespaceState[] _namespaces = [new(EventStoreNamespaceName.Default, DateTimeOffset.UnixEpoch)];

    void Establish()
    {
        _model = Definitions.CreateReadModelDefinition("model") with
        {
            ObserverIdentifier = "producer",
            Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.InMemory)
        };
        _projection = Definitions.CreateDefinition("producer", "model") with { Owner = ProjectionOwner.Client, EventSequenceId = "event-log" };
        _models = [_model];
        _projections = [_projection];
        _storage = Substitute.For<IStorage>();
        _eventStore = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(Store).Returns(_eventStore);
        _eventStore.IsSinkProvenanceInventoryComplete().Returns(_ => Task.FromResult(_inventoryComplete));
        _eventStore.MarkSinkProvenanceInventoryComplete().Returns(_ =>
        {
            _inventoryComplete = true;
            return Task.CompletedTask;
        });
        _eventStore.ReadModels.GetAll().Returns(_ => Task.FromResult<IEnumerable<ReadModelDefinition>>(_models));
        _eventStore.Projections.GetAll().Returns(_ => Task.FromResult<IEnumerable<ProjectionDefinition>>(_projections));
        _eventStore.Reducers.GetAll().Returns(Task.FromResult<IEnumerable<ReducerDefinition>>([]));
        _eventStore.ReadModels.Has(Arg.Any<ReadModelIdentifier>()).Returns(call => Task.FromResult(_models.Any(model => model.Identifier == call.Arg<ReadModelIdentifier>())));
        _eventStore.ReadModels.Get(Arg.Any<ReadModelIdentifier>()).Returns(call => Task.FromResult(_models.Single(model => model.Identifier == call.Arg<ReadModelIdentifier>())));
        _eventStore.ProjectionDefinitionHistory.GetFor(Arg.Any<ProjectionId>()).Returns(_ => Task.FromResult<IEnumerable<ProjectionDefinitionHistoryEntry>>(_history));
        _eventStore.Namespaces.GetAll().Returns(_ => Task.FromResult<IEnumerable<NamespaceState>>(_namespaces));
        _eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(call => Namespace(call.Arg<EventStoreNamespaceName>()));
        _provenance = new(_storage);
    }

    protected ISinkProvenanceStorage Marker(EventStoreNamespaceName @namespace, SinkConfigurationId? configuration = null)
    {
        var key = (@namespace, configuration ?? SinkConfigurationId.None);
        if (!_markers.TryGetValue(key, out var marker)) _markers[key] = marker = new InMemorySinkProvenanceStorage();
        return marker;
    }

    IEventStoreNamespaceStorage Namespace(EventStoreNamespaceName @namespace)
    {
        var storage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.Sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(call => Marker(@namespace, call.Arg<ReadModelDefinition>().Sink.Configuration));
        return storage;
    }
}
