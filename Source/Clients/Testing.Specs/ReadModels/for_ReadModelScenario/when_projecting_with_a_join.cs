// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that a cross-source <c>[Join]</c> enriches the intended read-model instance, even when the
/// join-source event is seeded before the entity-under-test — the join source must not shadow the entity.
/// </summary>
public class when_projecting_with_a_join : Specification
{
    ReadModelScenario<JoinOrderSummary> _scenario;
    EventSourceId _orderId;
    Guid _orderGuid;
    Guid _customerGuid;
    JoinOrderSummary? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<JoinOrderSummary>();
        _orderGuid = Guid.NewGuid();
        _orderId = new EventSourceId(_orderGuid);
        _customerGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        // Seed the JOIN SOURCE (customer) first — deliberately exercising the order that previously let
        // the join-source event shadow the entity-under-test. InstanceForEventSourceId resolves the
        // intended order instance regardless of seed order.
        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new JoinCustomerRegistered("Ada"));

        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_resolve_the_order_instance() => _order.ShouldNotBeNull();
    [Fact] void should_key_the_order_by_its_own_id() => _order!.Id.ShouldEqual(_orderGuid);
    [Fact] void should_have_the_amount() => _order!.Amount.ShouldEqual(100m);
    [Fact] void should_join_the_customer_name() => _order!.CustomerName.ShouldEqual("Ada");
}
