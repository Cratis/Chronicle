// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_records_cannot_be_relied_on : given.a_manager_reading_durable_records
{
    async Task Establish()
    {
        await CreateManager();
        _recordsCanBeRelied = false;
        _observersAreSubscribed = true;
        foreach (var name in Namespaces)
        {
            _recorded.Add(name);
        }

        await _manager.Add(Definition());
    }

    async Task Because() => await Tick();

    [Fact] void should_not_read_the_records() => _records.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverKey>());
    [Fact] void should_ask_every_observer_whether_it_is_subscribed() => Namespaces.All(name => ObserverOf(name).ReceivedCalls().Any()).ShouldBeTrue();
    [Fact] void should_not_subscribe_any_namespace() => Namespaces.Any(WasSubscribed).ShouldBeFalse();
}
