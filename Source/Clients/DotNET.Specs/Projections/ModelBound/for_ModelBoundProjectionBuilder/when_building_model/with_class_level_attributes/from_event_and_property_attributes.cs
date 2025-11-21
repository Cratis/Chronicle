// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_class_level_attributes;

public class from_event_and_property_attributes : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(ProductRegisteredInInventory),
            typeof(ItemsAddedToInventory),
            typeof(ItemsRemovedFromInventory)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(InventoryStatus));

    [Fact] void should_have_three_from_definitions() => _result.From.Count.ShouldEqual(3);

    [Fact] void should_have_from_definition_for_product_registered()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegisteredInInventory)).ToContract();
        _result.From.Keys.Count(et => et.Id == eventType.Id && et.Generation == eventType.Generation).ShouldEqual(1);
    }

    [Fact] void should_have_from_definition_for_items_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ItemsAddedToInventory)).ToContract();
        _result.From.Keys.Count(et => et.Id == eventType.Id && et.Generation == eventType.Generation).ShouldEqual(1);
    }

    [Fact] void should_have_from_definition_for_items_removed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ItemsRemovedFromInventory)).ToContract();
        _result.From.Keys.Count(et => et.Id == eventType.Id && et.Generation == eventType.Generation).ShouldEqual(1);
    }

    [Fact] void should_map_product_name_from_product_registered()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ProductRegisteredInInventory)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(InventoryStatus.ProductName));
    }

    [Fact] void should_map_current_stock_with_add_from_items_added()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ItemsAddedToInventory)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(InventoryStatus.CurrentStock));
        var expression = properties[nameof(InventoryStatus.CurrentStock)];
        expression.ShouldContain(WellKnownExpressions.Add);
    }

    [Fact] void should_map_current_stock_with_subtract_from_items_removed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(ItemsRemovedFromInventory)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(InventoryStatus.CurrentStock));
        var expression = properties[nameof(InventoryStatus.CurrentStock)];
        expression.ShouldContain(WellKnownExpressions.Subtract);
    }

    [Fact] void should_have_all_definition() => _result.All.ShouldNotBeNull();

    [Fact] void should_have_last_updated_in_all_definition()
    {
        _result.All.Properties.Keys.ShouldContain(nameof(InventoryStatus.LastUpdated));
    }

    [Fact] void should_map_last_updated_to_event_context_occurred()
    {
        var expression = _result.All.Properties[nameof(InventoryStatus.LastUpdated)];
        expression.ShouldContain(WellKnownExpressions.EventContext);
        expression.ShouldContain(nameof(EventContext.Occurred));
    }
}
