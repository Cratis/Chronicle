// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Projections.Engine.Expressions.EventValues;
using Cratis.Chronicle.Projections.Engine.Expressions.Keys;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.given;

/// <summary>
/// A <see cref="Projection"/> grain running the real projection engine for an order whose lines are keyed children,
/// updated and removed by their own events and joined with the product they refer to (Cratis/Chronicle#4727).
/// </summary>
public class an_order_projection_grain_with_lines_joined_to_products : Specification
{
    protected const string EventStore = "the-event-store";
    protected const string ReadModel = "Proof.Pdl.Order";
    protected const string Order = "00000000-0000-0000-0000-000000000001";
    protected const string Kettle = "00000000-0000-0000-0000-000000000011";
    protected const string Mug = "00000000-0000-0000-0000-000000000012";

    protected virtual string Declaration => """
        projection Order => Proof.Pdl.Order
          every
            LastUpdated = $eventContext.occurred
          from OrderPlaced
            CustomerName = CustomerName
            IsShipped = false
          from OrderShipped
            IsShipped = true
          from ItemAddedToOrder
            increment ItemCount
          from ItemRemovedFromOrder
            decrement ItemCount
          from ItemQuantityAdjusted
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
            from ItemQuantityAdjusted key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
            remove with ItemRemovedFromOrder key ProductId
              parent $eventContext.eventSourceId
            join Product on ProductId
              with ProductRegistered
                ProductName = Name
              with ProductRenamed
                ProductName = Name
          remove with OrderCancelled
        """;

    protected Projection _grain;
    protected TestKitSilo _silo;
    protected EventSequenceStorage _eventSequenceStorage;

    readonly DateTimeOffset _start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    ulong _nextSequenceNumber;

    async Task Establish()
    {
        _silo = new TestKitSilo();
        _eventSequenceStorage = new EventSequenceStorage(EventStore, EventStoreNamespaceName.Default, EventSequenceId.Log, new IdentityStorage());

        var typeFormats = new TypeFormats();
        var keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var eventValueProviderExpressionResolvers = new EventValueProviderExpressionResolvers(typeFormats, NullLogger<EventValueProviderExpressionResolvers>.Instance);

        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([ReadModelDefinition]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.GetLatestForAllEventTypes().Returns(EventTypeSchemas);
        storage.GetEventStore(Arg.Any<EventStoreName>())
            .GetNamespace(Arg.Any<EventStoreNamespaceName>())
            .GetEventSequence(Arg.Any<EventSequenceId>())
            .Returns(_eventSequenceStorage);

        _silo.AddService<IProjectionFactory>(new ProjectionFactory(
            new ReadModelPropertyExpressionResolvers(eventValueProviderExpressionResolvers, typeFormats, NullLogger<ReadModelPropertyExpressionResolvers>.Instance),
            eventValueProviderExpressionResolvers,
            new KeyExpressionResolvers(eventValueProviderExpressionResolvers, keyResolvers, NullLogger<KeyExpressionResolvers>.Instance),
            new ExpandoObjectConverter(typeFormats),
            keyResolvers,
            storage,
            NullLogger<ProjectionFactory>.Instance));
        _silo.AddService(Substitute.For<IProjectionDefinitionComparer>());
        _silo.AddService<IObjectComparer>(new ObjectComparer());
        _silo.AddService(storage);
        _silo.AddService(Options.Create(new ChronicleOptions()));

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => namespacesGrain);

        var readModelGrain = Substitute.For<IReadModel>();
        readModelGrain.GetDefinition().Returns(Task.FromResult(ReadModelDefinition));
        _silo.AddProbe(_ => readModelGrain);

        var stateStorage = Substitute.For<IStorage<ProjectionDefinition>>();
        stateStorage.State = CompileDeclaration();
        _silo.Options.StorageFactory = _ => stateStorage;

        _grain = await _silo.CreateGrainAsync<Projection>(new ProjectionKey(ReadModel, EventStore).ToString());
    }

    /// <summary>
    /// Gets the read model the order projection projects to.
    /// </summary>
    protected static ReadModelDefinition ReadModelDefinition { get; } = new(
        ReadModel,
        "orders",
        "Order",
        ReadModelOwner.Client,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        ReadModelObserverIdentifier.Unspecified,
        SinkDefinition.None,
        new Dictionary<ReadModelGeneration, JsonSchema>
        {
            [ReadModelGeneration.First] = JsonSchema.FromJson("""
                {
                  "type": "object",
                  "properties": {
                    "id": { "type": "string", "format": "guid" },
                    "CustomerName": { "type": "string" },
                    "ProductId": { "type": "string", "format": "guid" },
                    "ProductName": { "type": "string" },
                    "IsShipped": { "type": "boolean" },
                    "ItemCount": { "type": "integer" },
                    "LastUpdated": { "type": "string", "format": "date-time" },
                    "Lines": {
                      "type": "array",
                      "items": {
                        "type": "object",
                        "properties": {
                          "ProductId": { "type": "string", "format": "guid" },
                          "Quantity": { "type": "integer" },
                          "ProductName": { "type": "string" }
                        }
                      }
                    }
                  }
                }
                """)
        },
        []);

    /// <summary>
    /// Gets the schemas of the event types the order projection handles.
    /// </summary>
    protected static IEnumerable<EventTypeSchema> EventTypeSchemas { get; } =
    [
        EventTypeSchemaFor("ProductRegistered", """{ "Name": { "type": "string" } }"""),
        EventTypeSchemaFor("ProductRenamed", """{ "Name": { "type": "string" } }"""),
        EventTypeSchemaFor("OrderPlaced", """{ "CustomerName": { "type": "string" }, "ProductId": { "type": "string", "format": "guid" } }"""),
        EventTypeSchemaFor("ItemAddedToOrder", """{ "ProductId": { "type": "string", "format": "guid" }, "Quantity": { "type": "integer" } }"""),
        EventTypeSchemaFor("ItemQuantityAdjusted", """{ "ProductId": { "type": "string", "format": "guid" }, "Quantity": { "type": "integer" } }"""),
        EventTypeSchemaFor("ItemRemovedFromOrder", """{ "ProductId": { "type": "string", "format": "guid" } }"""),
        EventTypeSchemaFor("OrderShipped", "{}"),
        EventTypeSchemaFor("OrderCancelled", "{}")
    ];

    protected Task<AppendedEvent> ProductRegistered(string product, string name) => Append(product, "ProductRegistered", new { Name = name });

    protected Task<AppendedEvent> ProductRenamed(string product, string name) => Append(product, "ProductRenamed", new { Name = name });

    protected Task<AppendedEvent> OrderPlaced(string customerName) => Append(Order, "OrderPlaced", new { CustomerName = customerName });

    protected Task<AppendedEvent> ItemAddedToOrder(string product, int quantity) => Append(Order, "ItemAddedToOrder", new { ProductId = Guid.Parse(product), Quantity = quantity });

    protected Task<AppendedEvent> ItemQuantityAdjusted(string product, int quantity) => Append(Order, "ItemQuantityAdjusted", new { ProductId = Guid.Parse(product), Quantity = quantity });

    protected Task<AppendedEvent> ItemRemovedFromOrder(string product) => Append(Order, "ItemRemovedFromOrder", new { ProductId = Guid.Parse(product) });

    protected Task<AppendedEvent> OrderShipped() => Append(Order, "OrderShipped", new { });

    /// <summary>
    /// Previews the events through the path a draft or inferred read model takes.
    /// </summary>
    /// <param name="events">The events to preview.</param>
    /// <returns>The previewed read models.</returns>
    protected Task<IEnumerable<ExpandoObject>> Preview(params AppendedEvent[] events) =>
        _grain.ProcessForPreview(EventStoreNamespaceName.Default, events, ReadModelDefinition);

    /// <summary>
    /// Gets the read model with the given identifier.
    /// </summary>
    /// <param name="readModels">The read models.</param>
    /// <param name="id">The identifier.</param>
    /// <returns>The read model as a dictionary.</returns>
    protected static IDictionary<string, object?> ReadModelFor(IEnumerable<ExpandoObject> readModels, string id) =>
        readModels.Cast<IDictionary<string, object?>>().Single(_ => Equals(_["id"]?.ToString(), id));

    /// <summary>
    /// Gets the lines of a read model.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <returns>The lines as dictionaries.</returns>
    protected static IDictionary<string, object?>[] LinesOf(IDictionary<string, object?> readModel) =>
        readModel.TryGetValue("Lines", out var lines) && lines is IEnumerable<object> items
            ? [.. items.Cast<IDictionary<string, object?>>()]
            : [];

    static EventTypeSchema EventTypeSchemaFor(string eventType, string properties) => new(
        new EventType(eventType, EventTypeGeneration.First),
        EventTypeOwner.Client,
        EventTypeSource.Code,
        JsonSchema.FromJson($$"""{ "type": "object", "properties": {{properties}} }"""));

    ProjectionDefinition CompileDeclaration()
    {
        var language = new LanguageService(new Generator(), Engine.DeclarationLanguage.CodeGeneration.given.ProjectionCodeGenerators.All());
        return language.Compile(Declaration, ProjectionOwner.Server, [], []).Match(
            definition => definition with { Identifier = ReadModel },
            errors => throw new InvalidOperationException($"Compilation failed: {string.Join(", ", errors.Errors)}"));
    }

    protected async Task<AppendedEvent> Append(EventSourceId eventSourceId, string eventType, object content)
    {
        var sequenceNumber = _nextSequenceNumber++;
        var result = await _eventSequenceStorage.Append(
            sequenceNumber,
            EventSourceType.Default,
            eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            new EventType(eventType, EventTypeGeneration.First),
            CorrelationId.New(),
            [],
            [],
            [],
            _start.AddSeconds(sequenceNumber),
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = content.AsExpandoObject() },
            new Dictionary<EventTypeGeneration, EventHash>());
        return _eventSequenceStorage.Events.Single(_ => _.Context.SequenceNumber == sequenceNumber);
    }
}
