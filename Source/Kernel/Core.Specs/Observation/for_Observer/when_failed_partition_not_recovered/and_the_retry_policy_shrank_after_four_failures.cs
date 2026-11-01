// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_not_recovered;

/// <summary>
/// A recovery that does not recover the partition rearms the retry without checking the attempt limit. When the retry
/// policy changed in the meantime, the computed backoff can overflow, and must then be the maximum backoff delay.
/// </summary>
public class and_the_retry_policy_shrank_after_four_failures : given.an_observer
{
    async Task Establish()
    {
        for (var index = 0; index < 4; index++)
        {
            await _observer.PartitionFailed("partition", 42UL, ["failure"], "", FailureKind.Handling);
        }

        _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { WriteCoalescingInterval = 0, MaxRetryAttempts = 1, ExponentialBackoffDelayFactor = 1e8f });
    }

    async Task Because() => await _observer.FailedPartitionNotRecovered("partition");

    [Fact] void should_register_the_retry_reminder_with_the_maximum_backoff_delay() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.IsAny<string>(), TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(600)), Times.Once);
    [Fact] void should_keep_the_partition_failed() => _failedPartitionsState.Partitions.Select(_ => _.Partition.ToString()).ShouldContain("partition");
}
