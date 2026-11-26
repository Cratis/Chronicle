// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_HandleEvent.given;

public class a_root_join_with_initial_values : Specification
{
    static readonly EventType _orderPlaced = new("OrderPlaced", EventTypeGeneration.First);
    static readonly EventType _customerNamed = new("CustomerNamed", EventTypeGeneration.First);
    static readonly Key _orderKey = new("order-a", ArrayIndexers.NoIndexers);
    IProjection _projection;
    IEventSequenceStorage _eventSequenceStorage;
    InMemorySink _sink;
    protected AppendedEvent _joinedEvent;
    protected ProjectionEventContext _context;
    protected IDictionary<string, object?> Order => _sink.Collection[_orderKey.Value];

    async Task Establish()
    {
        EventStoreName eventStore = "event-store";
        EventStoreNamespaceName @namespace = "namespace";
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(EventSequenceId.Log).Returns(_eventSequenceStorage);
        var formats = new TypeFormats();
        var keys = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var values = new EventValueProviderExpressionResolvers(formats, NullLogger<EventValueProviderExpressionResolvers>.Instance);
        var factory = new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(values, formats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            values,
            new KeyExpressionResolvers(values, keys, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(formats),
            keys,
            storage,
            NullLogger<ProjectionFactory>.Instance);
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"customerId":{"type":"string"},"customerName":{"type":["string","null"]},"status":{"type":"string"}}}
            """);
        var readModel = new ReadModelDefinition(
            "Order",
            "Order",
            "Order",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema },
            []);
        _projection = await factory.Create(eventStore, @namespace, CreateDefinition(), readModel, []);
        _sink = new InMemorySink(readModel, formats);
        _joinedEvent = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("customer-c") with { EventType = _customerNamed, SequenceNumber = 1 },
            new { name = "Alice" }.AsExpandoObject());
        _eventSequenceStorage.TryGetLastEventBefore(_customerNamed.Id, "customer-c", Arg.Any<EventSequenceNumber>())
            .Returns(_ => Task.FromResult(Catch<Option<AppendedEvent>>.Success(new Option<AppendedEvent>(_joinedEvent))));
    }

    protected void StoreExistingOrder() => _sink.Collection[_orderKey.Value] = new
    {
        id = "order-a",
        customerId = "previous-customer",
        customerName = "before",
        status = "existing"
    }.AsExpandoObject();

    protected async Task ProjectOrder()
    {
        var incoming = new AppendedEvent(
            EventContext.EmptyWithEventSourceId("order-a") with { EventType = _orderPlaced, SequenceNumber = 2 },
            new { customerId = "customer-c" }.AsExpandoObject());
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), incoming, new ExpandoObject());
        _context = new ProjectionEventContext(_orderKey, incoming, changeset, ProjectionOperationType.From, false);
        _context = await new SetInitialState(_sink, NullLogger<SetInitialState>.Instance).Perform(_projection, _context);
        _context = await new HandleEvent(_eventSequenceStorage, _sink, NullLogger<HandleEvent>.Instance).Perform(_projection, _context);
        var failures = await _sink.ApplyChanges(_orderKey, changeset, incoming.Context.SequenceNumber);
        failures.ShouldBeEmpty();
    }

    static ProjectionDefinition CreateDefinition() => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        "order-projection",
        "Order",
        true,
        true,
        new JsonObject { ["customerName"] = "seed", ["status"] = "pending" },
        new Dictionary<EventType, FromDefinition>
        {
            [_orderPlaced] = new(new Dictionary<PropertyPath, string> { ["customerId"] = "customerId" }, "$eventSourceId", null)
        },
        new Dictionary<EventType, JoinDefinition>
        {
            [_customerNamed] = new("customerId", new Dictionary<PropertyPath, string> { ["customerName"] = "name" }, "$eventSourceId")
        },
        new Dictionary<PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<PropertyPath, string>(), false),
        new Dictionary<EventType, RemovedWithDefinition>(),
        new Dictionary<EventType, RemovedWithJoinDefinition>(),
        AutoMap: AutoMap.Disabled);

    void Destroy() => _sink.Dispose();
}
