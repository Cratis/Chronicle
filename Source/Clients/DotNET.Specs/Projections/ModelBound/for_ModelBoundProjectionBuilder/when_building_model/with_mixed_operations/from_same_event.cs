// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_mixed_operations;

public class from_same_event : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(OrderPlaced)
        ]);

        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(OrderSummary));

    [Fact] void should_have_single_from_definition() => _result.From.Count.ShouldEqual(1);

    [Fact] void should_have_from_definition_for_order_placed()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderPlaced)).ToContract();
        _result.From.Keys.Count(et => et.Id == eventType.Id && et.Generation == eventType.Generation).ShouldEqual(1);
    }

    [Fact] void should_have_three_properties_in_definition()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderPlaced)).ToContract();
        var entry = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation);
        entry.Value.Properties.Count.ShouldEqual(3);
    }

    [Fact] void should_map_customer_name_with_set()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderPlaced)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(OrderSummary.CustomerName));
        properties[nameof(OrderSummary.CustomerName)].ShouldNotContain(WellKnownExpressions.Add);
    }

    [Fact] void should_map_total_quantity_with_add()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderPlaced)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(OrderSummary.TotalQuantity));
        properties[nameof(OrderSummary.TotalQuantity)].ShouldContain(WellKnownExpressions.Add);
    }

    [Fact] void should_map_total_revenue_with_add()
    {
        var eventType = event_types.GetEventTypeFor(typeof(OrderPlaced)).ToContract();
        var properties = _result.From.First(kvp => kvp.Key.Id == eventType.Id && kvp.Key.Generation == eventType.Generation).Value.Properties;
        properties.Keys.ShouldContain(nameof(OrderSummary.TotalRevenue));
        properties[nameof(OrderSummary.TotalRevenue)].ShouldContain(WellKnownExpressions.Add);
    }
}
