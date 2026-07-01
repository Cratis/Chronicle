// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_from_event_shares_a_property_with_a_join_source;

/// <summary>
/// Verifies that with the join source present, the join-sourced property is populated from the joined-in
/// customer — the value comes from the join, never the order's coincidental same-named property.
/// </summary>
public class and_the_join_source_is_present : Specification
{
    ReadModelScenario<RegionJoinSummary> _scenario;
    EventSourceId _orderId;
    Guid _customerGuid;
    RegionJoinSummary? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<RegionJoinSummary>();
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

    [Fact] void should_join_the_customer_region() => _order!.Region.ShouldEqual("CustomerRegion");
}
