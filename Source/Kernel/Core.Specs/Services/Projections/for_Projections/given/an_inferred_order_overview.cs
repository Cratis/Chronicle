// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.given;

public class an_inferred_order_overview : an_inferred_read_model_preview
{
    protected const string Declaration = """
        projection OrderOverview
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

          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              Quantity = Quantity
            from ItemQuantityAdjusted key ProductId
              Quantity = Quantity
            remove with ItemRemovedFromOrder key ProductId
            join Product on ProductId
              with ProductRegistered
                ProductName = Name
              with ProductRenamed
                ProductName = Name

          remove with OrderCancelled
        """;

    protected IReadModelsManager _readModelsManager;
    protected IProjectionsManager _projectionsManager;

    void Establish()
    {
        _eventTypesStorage.GetLatestForAllEventTypes().Returns([
            EventSchema("OrderPlaced", """{ "CustomerName": { "type": "string" } }"""),
            EventSchema("OrderShipped", "{}"),
            EventSchema("ItemAddedToOrder", """{ "ProductId": { "type": "string" }, "Quantity": { "type": "integer" } }"""),
            EventSchema("ItemQuantityAdjusted", """{ "ProductId": { "type": "string" }, "Quantity": { "type": "integer" } }"""),
            EventSchema("ItemRemovedFromOrder", """{ "ProductId": { "type": "string" } }"""),
            EventSchema("OrderCancelled", "{}"),
            EventSchema("ProductRegistered", """{ "Name": { "type": "string" } }"""),
            EventSchema("ProductRenamed", """{ "Name": { "type": "string" } }""")
        ]);
        _readModelsManager = Substitute.For<IReadModelsManager>();
        _grainFactory.GetGrain<IReadModelsManager>(Arg.Any<string>()).Returns(_readModelsManager);
        _projectionsManager = Substitute.For<IProjectionsManager>();
        _projectionsManager.GetProjectionDefinitions().Returns([]);
        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_projectionsManager);
    }

    static EventTypeSchema EventSchema(string name, string properties) => new(
        new EventType(name, EventTypeGeneration.First),
        EventTypeOwner.Client,
        EventTypeSource.Code,
        JsonSchema.FromJson($$"""{ "type": "object", "properties": {{properties}} }"""));
}
