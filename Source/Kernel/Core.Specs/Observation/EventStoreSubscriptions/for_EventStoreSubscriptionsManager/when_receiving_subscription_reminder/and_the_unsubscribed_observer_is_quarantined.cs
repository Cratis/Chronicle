// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_the_unsubscribed_observer_is_quarantined : given.a_manager_with_a_quarantined_subscription
{
    async Task Because()
    {
        await _manager.ReceiveReminder(ReminderName, default);
        await _manager.ReceiveReminder(ReminderName, default);
    }

    [Fact] void should_reconcile_the_subscription_once() => ShouldSubscribe(_observer);
    [Fact] void should_not_explicitly_subscribe() => ShouldNotSubscribe(_observer);
    [Fact] void should_keep_scheduling_the_reminder() => ShouldScheduleReminder(3);
}
