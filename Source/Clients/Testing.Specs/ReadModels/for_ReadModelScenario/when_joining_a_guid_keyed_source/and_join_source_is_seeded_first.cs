// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_guid_keyed_source;

/// <summary>
/// A root <c>[Join]</c> keyed by its own Guid identifier backfills the joined value when the join-source event
/// is seeded BEFORE the row-creating FROM event (the join-source-first order). This is the ordering the engine's
/// row-creation-time backfill (ResolveJoin) already handles; asserting it here proves the entity-first fix does
/// not regress it — both orders produce IDENTICAL enriched results.
/// </summary>
public class and_join_source_is_seeded_first : Specification
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
        // Seed the JOIN SOURCE (customer) first — it exists by the time the order row is created.
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
    [Fact] void should_backfill_the_customer_name() => _order!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_materialize_only_the_order_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
