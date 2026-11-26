// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_one_namespace_has_no_durable_record : given.a_manager_reading_durable_records
{
    async Task Establish()
    {
        await CreateManager();
        _recorded.Add("Default");
        _recorded.Add("Tenant2");
        await _manager.Add(Definition());
    }

    async Task Because() => await Tick();

    [Fact] void should_ask_the_observer_without_a_record_whether_it_is_subscribed() => ObserverOf("Tenant1").Received().IsSubscribed();
    [Fact] void should_not_resolve_the_observer_of_the_first_recorded_namespace() => _observers.ContainsKey(KeyFor("Default").ToString()).ShouldBeFalse();
    [Fact] void should_not_resolve_the_observer_of_the_second_recorded_namespace() => _observers.ContainsKey(KeyFor("Tenant2").ToString()).ShouldBeFalse();
    [Fact] void should_subscribe_the_namespace_without_a_record() => WasSubscribed("Tenant1").ShouldBeTrue();
}
