// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_has_an_unlimited_retry_budget : given.a_webhook_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 0, BackoffDelay = 1, ExponentialBackoffDelayFactor = float.MaxValue, MaximumBackoffDelay = 10 };

    void Establish()
    {
        for (var attempt = 0; attempt < 9; attempt++)
        {
            _failedPartitionsState.RegisterAttempt("partition", 42UL, ["503"], "", FailureKind.Handling);
        }
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["503"], "", FailureKind.Handling);

    [Fact] void should_not_park_the_partition() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeFalse();
    [Fact] void should_cap_backoff_even_after_exponential_overflow() => (_failedPartitionsState.Partitions.Single().Deferral!.Until - _failedPartitionsState.Partitions.Single().Deferral!.Since).TotalSeconds.ShouldBeInRange(8d, 10d);
}
