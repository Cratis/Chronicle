// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_from_all;

public class and_a_fluent_from_all_is_combined_with_a_join : Specification
{
    ReadModelScenario<FluentJoinedAllEvents> _scenario;
    EventSourceId _orderId;
    EventSourceId _customerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<FluentJoinedAllEvents>().WithStrictEventSubscription();
        _orderId = EventSourceId.New();
        _customerId = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_orderId).Events(new JoinOrderPlaced(new JoinCustomerId(Guid.Parse(_customerId.Value)), 100m));
        await _scenario.Given.ForEventSource(_customerId).Events(new JoinCustomerRegistered("Ada"));
    }

    [Fact] void should_join_the_customer_name() => _scenario.InstanceForEventSourceId(_orderId)!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_count_the_join_event_only_once() => _scenario.InstanceForEventSourceId(_orderId)!.EventCount.ShouldEqual(2);
}
