// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_guid_keyed_source;

/// <summary>
/// A root <c>[Join]</c> keyed by its own Guid identifier backfills the joined value when the row-creating
/// FROM event is seeded BEFORE the join-source event (the entity-first order). The join source arriving after
/// the row already exists must enrich the EXISTING root document — mirroring the real engine / MongoDB — rather
/// than writing a phantom document keyed by the join source's own id.
/// </summary>
public class and_entity_is_seeded_first : Specification
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
        // Seed the ENTITY (order) first — the row-creating FROM event arrives before the join source exists.
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));

        // Then the JOIN SOURCE (customer) — it must enrich the existing order row, not create a phantom.
        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new JoinCustomerRegistered("Ada"));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_resolve_the_order_instance() => _order.ShouldNotBeNull();
    [Fact] void should_key_the_order_by_its_own_id() => _order!.Id.ShouldEqual(_orderGuid);
    [Fact] void should_have_the_amount() => _order!.Amount.ShouldEqual(100m);
    [Fact] void should_backfill_the_customer_name() => _order!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_materialize_only_the_order_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
