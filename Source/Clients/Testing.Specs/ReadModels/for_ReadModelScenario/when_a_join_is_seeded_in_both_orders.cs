// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Seeds the same <c>[Join]</c> scenario in both orders — entity-then-join-source and
/// join-source-then-entity — using identical ids and asserts they materialize the same instance. The join
/// must not depend on the order events were seeded in, matching the runtime where a join event enriches its
/// target whenever it arrives.
/// </summary>
public class when_a_join_is_seeded_in_both_orders : Specification
{
    EventSourceId _orderId;
    Guid _orderGuid;
    Guid _customerGuid;
    JoinOrderSummary? _entityFirst;
    JoinOrderSummary? _joinSourceFirst;

    void Establish()
    {
        _orderGuid = Guid.NewGuid();
        _orderId = new EventSourceId(_orderGuid);
        _customerGuid = Guid.NewGuid();
    }

    async Task Because()
    {
        _entityFirst = await Project(entityFirst: true);
        _joinSourceFirst = await Project(entityFirst: false);
    }

    [Fact] void should_resolve_the_entity_when_seeded_entity_first() => _entityFirst.ShouldNotBeNull();
    [Fact] void should_resolve_the_entity_when_seeded_join_source_first() => _joinSourceFirst.ShouldNotBeNull();
    [Fact] void should_key_both_by_the_same_order_id() => _entityFirst!.Id.ShouldEqual(_joinSourceFirst!.Id);
    [Fact] void should_carry_the_same_amount() => _entityFirst!.Amount.ShouldEqual(_joinSourceFirst!.Amount);
    [Fact] void should_join_the_same_customer_name() => _entityFirst!.CustomerName.ShouldEqual(_joinSourceFirst!.CustomerName);
    [Fact] void should_join_the_customer_name_regardless_of_order() => _entityFirst!.CustomerName.ShouldEqual("Ada");

    async Task<JoinOrderSummary?> Project(bool entityFirst)
    {
        var scenario = new ReadModelScenario<JoinOrderSummary>();
        var customerId = new EventSourceId(_customerGuid);

        if (entityFirst)
        {
            await scenario.Given.ForEventSource(_orderId).Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));
            await scenario.Given.ForEventSource(customerId).Events(new JoinCustomerRegistered("Ada"));
        }
        else
        {
            await scenario.Given.ForEventSource(customerId).Events(new JoinCustomerRegistered("Ada"));
            await scenario.Given.ForEventSource(_orderId).Events(new JoinOrderPlaced(new JoinCustomerId(_customerGuid), 100m));
        }

        return scenario.InstanceForEventSourceId(_orderId);
    }
}
