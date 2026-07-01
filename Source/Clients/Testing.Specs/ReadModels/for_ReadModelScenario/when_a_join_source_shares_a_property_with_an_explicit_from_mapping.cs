// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that when a joined-in event carries a property whose name collides with an explicitly-sourced
/// read-model property, the join's name-based AutoMap does not bleed the joined value over the explicit
/// source — while the join still pulls in the property it was declared for.
/// </summary>
public class when_a_join_source_shares_a_property_with_an_explicit_from_mapping : Specification
{
    ReadModelScenario<RegionOrderSummary> _scenario;
    EventSourceId _orderId;
    Guid _customerGuid;
    RegionOrderSummary? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<RegionOrderSummary>();
        _orderId = EventSourceId.New();
        _customerGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new RegionCustomerRegistered("CustomerRegion", "Bergen"));

        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new RegionOrderPlaced(new RegionCustomerId(_customerGuid), "OrderRegion"));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_keep_the_explicitly_sourced_region() => _order!.Region.ShouldEqual("OrderRegion");
    [Fact] void should_still_join_the_city() => _order!.City.ShouldEqual("Bergen");
}
