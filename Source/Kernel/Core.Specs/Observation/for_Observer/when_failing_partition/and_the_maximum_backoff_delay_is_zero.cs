// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// A maximum backoff delay of zero makes the retry due immediately, with the minimum reminder period.
/// </summary>
public class and_the_maximum_backoff_delay_is_zero : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaximumBackoffDelay = 0 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["failure"], "");

    [Fact] void should_register_the_retry_reminder_due_immediately() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.Zero, TimeSpan.FromMinutes(1)), Times.Once);
}
