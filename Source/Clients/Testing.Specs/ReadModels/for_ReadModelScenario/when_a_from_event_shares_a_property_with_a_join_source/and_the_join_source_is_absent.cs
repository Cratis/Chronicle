// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_from_event_shares_a_property_with_a_join_source;

/// <summary>
/// Verifies that when a join-sourced property collides by name with a property on the subscribed order
/// event, name-based AutoMap from the order does not bleed its value in — so with the join source absent
/// the property stays unset rather than taking the order's coincidental region.
/// </summary>
public class and_the_join_source_is_absent : Specification
{
    ReadModelScenario<RegionJoinSummary> _scenario;
    EventSourceId _orderId;
    RegionJoinSummary? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<RegionJoinSummary>();
        _orderId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new RegionOrderPlaced(new RegionCustomerId(Guid.NewGuid()), "OrderRegion"));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_materialize_the_order() => _order.ShouldNotBeNull();
    [Fact] void should_not_bleed_the_orders_region_over_the_join() => string.IsNullOrEmpty(_order!.Region).ShouldBeTrue();
}
