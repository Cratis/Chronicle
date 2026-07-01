// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Documents the seed-order requirement for a <c>[Join]</c> value: the entity pulls the joined value when
/// its own event is processed, so a join-source event seeded AFTER the entity resolves the entity's
/// identity but does not back-fill the joined value. Seed the join-source stream first for the value to
/// be populated. (If a future change back-fills the join, this spec is the canary — update the docs then.)
/// </summary>
public class when_a_join_source_is_seeded_after_the_entity : Specification
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
        await _scenario.Given
            .ForEventSource(_orderId)
            .Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));

        await _scenario.Given
            .ForEventSource(new EventSourceId(_customerGuid))
            .Events(new JoinCustomerRegistered("Ada"));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_still_resolve_the_entity_instance() => _order.ShouldNotBeNull();
    [Fact] void should_carry_the_entitys_own_data() => _order!.Amount.ShouldEqual(100m);
    [Fact] void should_not_back_fill_the_joined_value() => _order!.CustomerName.ShouldBeNull();
}
