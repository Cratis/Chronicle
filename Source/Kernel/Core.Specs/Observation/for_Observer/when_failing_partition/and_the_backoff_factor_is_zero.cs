// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// A backoff factor of zero computes no delay, so the retry falls back to the base backoff delay.
/// </summary>
public class and_the_backoff_factor_is_zero : given.an_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, BackoffDelay = 5, ExponentialBackoffDelayFactor = 0 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["failure"], "");

    [Fact] void should_register_the_retry_reminder_due_after_the_base_backoff_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1)), Times.Once);
}
