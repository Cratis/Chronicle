// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

/// <summary>
/// A failed partition is still something to report, so it keeps the durable wakeup, with its first tick spread over the period.
/// </summary>
public class and_a_partition_is_quarantined : given.a_quarantined_partition
{
    async Task Because() => await Crash();

    [Fact] void should_register_the_alert_reminder() => _silo.ReminderRegistry.Mock.Verify(registry => registry.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Observer.AlertReminderName, It.Is<TimeSpan>(due => due >= TimeSpan.FromMinutes(1) && due < TimeSpan.FromMinutes(2)), TimeSpan.FromMinutes(1)), Times.Once);
}
