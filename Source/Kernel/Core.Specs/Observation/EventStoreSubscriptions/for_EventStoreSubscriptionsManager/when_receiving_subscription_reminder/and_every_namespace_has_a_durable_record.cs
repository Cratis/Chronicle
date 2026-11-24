// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_every_namespace_has_a_durable_record : given.a_manager_reading_durable_records
{
    async Task Establish()
    {
        await CreateManager();
        foreach (var name in Namespaces)
        {
            _recorded.Add(name);
        }

        await _manager.Add(Definition());
    }

    async Task Because() => await Tick();

    [Fact] void should_not_call_any_observer() => ObserverCalls.ShouldEqual(0);
    [Fact] void should_not_resolve_any_observer() => _observers.ShouldBeEmpty();
    [Fact] void should_read_the_definition_of_the_observer_once() => _definitions.Received(1).Get(new ObserverId($"{SubscriptionName}->Target"));
}
