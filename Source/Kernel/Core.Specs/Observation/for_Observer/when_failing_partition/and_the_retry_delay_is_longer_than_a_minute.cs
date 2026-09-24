// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// The retry reminder is removed as soon as it is received, so its period only matters when its first tick is never
/// delivered. The next tick then comes one period later, which must be no later than the retry interval itself.
/// </summary>
public class and_the_retry_delay_is_longer_than_a_minute : given.an_observer
{
    const string Partition = "Something";

    protected override Observers CreateObserversConfig() => new() { BackoffDelay = 100 };

    async Task Because() => await _observer.PartitionFailed(Partition, 42UL, ["Something went wrong"], "This is the stack trace");

    [Fact] void should_register_a_retry_reminder_due_after_the_backoff_delay_that_repeats_at_the_same_interval() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), Partition, TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)), Times.Once);
}
