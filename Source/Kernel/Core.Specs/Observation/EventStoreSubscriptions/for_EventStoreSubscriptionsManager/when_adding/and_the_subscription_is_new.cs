// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsManager.when_adding;

public class and_the_subscription_is_new : given.a_manager_reading_durable_records
{
    const string ReminderName = "event-store-subscription-subscribe:subscription";

    async Task Establish() => await CreateManager();

    async Task Because() => await _manager.Add(Definition());

    [Fact]
    void should_check_it_at_once_and_then_every_safety_net_period() =>
        _silo.ReminderRegistry.Mock.Verify(
            registry => registry.RegisterOrUpdateReminder(It.IsAny<Orleans.Runtime.GrainId>(), ReminderName, TimeSpan.FromMilliseconds(100), TimeSpan.FromMinutes(15)),
            Times.Once);
}
