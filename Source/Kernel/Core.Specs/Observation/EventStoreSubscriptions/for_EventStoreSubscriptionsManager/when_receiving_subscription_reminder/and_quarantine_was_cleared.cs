// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_quarantine_was_cleared : given.a_manager_with_a_quarantined_subscription
{
    async Task Establish()
    {
        await _manager.ReceiveReminder(ReminderName, default);
        _observer.ClearReceivedCalls();
        _observer.IsObserverQuarantined().Returns(false);
    }

    async Task Because() => await _manager.ReceiveReminder(ReminderName, default);

    [Fact] void should_establish_the_subscription() => ShouldSubscribe(_observer);
    [Fact] void should_keep_scheduling_the_reminder() => ShouldScheduleReminder(3);
}
