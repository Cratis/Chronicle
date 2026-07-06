// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_a_property_is_marked_no_automap;

/// <summary>
/// Verifies that a property flagged with property-level <c>[NoAutoMap]</c> keeps its explicitly sourced
/// value when the read model also <c>[Join]</c>s an event that happens to carry a property with the same
/// name — the join enriches <c>PartnerName</c> without letting the partner's status overwrite the order
/// status. Exercises the join AutoMap path (<c>GetMergedJoinProperties</c>), the counterpart to the
/// <c>[Count]</c>/<c>From</c> case.
/// </summary>
public class and_a_joined_event_carries_the_same_property : Specification
{
    ReadModelScenario<PartneredOrderSummary> _scenario;
    EventSourceId _orderId;
    Guid _orderGuid;
    Guid _customerGuid;
    PartneredOrderSummary? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<PartneredOrderSummary>();
        _orderGuid = Guid.NewGuid();
        _orderId = new EventSourceId(_orderGuid);
        _customerGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new PartnerRegistered("Ada", "Gold"));

        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new PartneredOrderPlaced(new JoinCustomerId(_customerGuid), "Open"));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_resolve_the_order_instance() => _order.ShouldNotBeNull();
    [Fact] void should_keep_the_explicitly_sourced_status() => _order!.Status.ShouldEqual("Open");
    [Fact] void should_join_the_partner_name() => _order!.PartnerName.ShouldEqual("Ada");
}
