// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_event_types_changed_while_every_namespace_has_a_record : given.a_manager_reading_durable_records
{
    async Task Establish()
    {
        await CreateManager();
        foreach (var name in Namespaces)
        {
            _recorded.Add(name);
        }

        await _manager.Add(Definition());
        await _manager.Add(Definition(EventType, new EventType("0b3a0b0e-3a1f-4c7d-9d4a-5f1a7e9c2b11", EventTypeGeneration.First)));
    }

    async Task Because() => await Tick();

    [Fact] void should_subscribe_every_namespace_with_the_new_event_types() => Namespaces.All(WasSubscribed).ShouldBeTrue();
}
