// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Bindings.for_MaterializationKeys.given;

public class a_materialization : Specification
{
    protected IStorage _storage;
    protected IEventStoreStorage _store;
    protected MaterializationKeys _keys;
    protected EventStoreName _name = "orders";
    protected EventStoreNamespaceName _namespace = "tenant";
    protected ProjectionId _projection = "projection";
    protected SinkDefinition _sink = new(new(Guid.NewGuid()), "mongodb");
    protected GenerationFloor _floor = new(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.UnixEpoch, "spec", "spec");

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(_name).Returns(_store);
        _store.GenerationFloor.Get().Returns(_floor);
        _store.Projections.Get(_projection).Returns(new ProjectionDefinition(ProjectionOwner.Client, new("event-log"), _projection, new("model"), true, true, new(), new Dictionary<Cratis.Chronicle.Concepts.Events.EventType, FromDefinition>(), new Dictionary<Cratis.Chronicle.Concepts.Events.EventType, JoinDefinition>(), new Dictionary<Cratis.Chronicle.Properties.PropertyPath, ChildrenDefinition>(), [], new(new Dictionary<Cratis.Chronicle.Properties.PropertyPath, string>(), false), new Dictionary<Cratis.Chronicle.Concepts.Events.EventType, RemovedWithDefinition>(), new Dictionary<Cratis.Chronicle.Concepts.Events.EventType, RemovedWithJoinDefinition>()));
        _store.ReadModels.Get(new("model")).Returns(new ReadModelDefinition(new("model"), new("models"), new("Models"), ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, new("projection"), _sink, new Dictionary<ReadModelGeneration, Cratis.Chronicle.Schemas.JsonSchema>(), []));
        _keys = new(_storage);
    }
}
