// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Generations.Enablement.for_LegacyMaterializations.given;

public class materializations : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected IEventStoreNamespaceStorage _namespace;
    protected LegacyMaterializations _reader;
    protected IReadOnlyList<LegacyMaterialization> _actual;
    protected ReadModelDefinition _model;
    protected ProjectionDefinition _projection;
    protected ObserverState _observer;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _storage.GetEventStore(new("orders")).Returns(_store);
        _store.Namespaces.GetAll().Returns([new NamespaceState(new("tenant"), default)]);
        _store.GetNamespace(new("tenant")).Returns(_namespace);
        _projection = new(ProjectionOwner.Client, new("event-log"), new("projection"), new("orders-model"), true, true, new JsonObject(), new Dictionary<EventType, FromDefinition>(), new Dictionary<EventType, JoinDefinition>(), new Dictionary<PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<PropertyPath, string>(), false), new Dictionary<EventType, RemovedWithDefinition>(), new Dictionary<EventType, RemovedWithJoinDefinition>());
        _model = new(new("orders-model"), new("orders-current"), new("Orders"), ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, new("projection"), SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema>(), []);
        _observer = new(new("projection"), new(42), default, new HashSet<Key>(), new HashSet<Key>(), [], new(2), false, false);
        _store.Projections.GetAll().Returns([_projection]);
        _store.ReadModels.GetAll().Returns([_model]);
        _namespace.Observers.GetAll().Returns([_observer]);
        _reader = new(_storage);
    }
}
