// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_quarantined_subscription_has_stale_event_types : given.a_manager_with_a_quarantined_subscription
{
    void Establish()
    {
        _observer.IsSubscribed().Returns(true);
        _observer.GetEventTypes().Returns([new EventType("079e1a45-6461-4de5-a5e1-ed2fa15c57f6", EventTypeGeneration.First)]);
    }

    async Task Because()
    {
        await _manager.ReceiveReminder(ReminderName, default);
        _observer.IsObserverQuarantined().Returns(false);
        await _manager.ReceiveReminder(ReminderName, default);
    }

    [Fact] void should_reconcile_the_event_types_once() => ShouldSubscribe(_observer);
    [Fact] void should_not_explicitly_subscribe() => ShouldNotSubscribe(_observer);
    [Fact] async Task should_apply_the_current_event_types() => (await _observer.GetEventTypes()).ShouldEqual([EventType.Unknown]);
    [Fact] void should_keep_scheduling_the_reminder() => ShouldScheduleReminder(3);
}
