// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_receiving_subscription_reminder;

public class and_it_has_checked_the_subscription : given.a_manager_reading_durable_records
{
    const string ReminderName = "event-store-subscription-subscribe:subscription";

    async Task Establish()
    {
        await CreateManager();
        await _manager.Add(Definition());
        _silo.ReminderRegistry.Mock.Invocations.Clear();
    }

    async Task Because() => await Tick();

    [Fact]
    void should_schedule_the_next_check_a_whole_safety_net_period_away() =>
        _silo.ReminderRegistry.Mock.Verify(
            registry => registry.RegisterOrUpdateReminder(It.IsAny<Orleans.Runtime.GrainId>(), ReminderName, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15)),
            Times.Once);
}
