// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_keeps_timing_out : given.a_webhook_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 2 };

    async Task Establish() => await _observer.PartitionFailed("partition", 42UL, ["timed out"], string.Empty, FailureKind.Timeout);

    async Task Because()
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var failure = _failedPartitionsState.Partitions.Single();
            await _observer.PartitionFailed("partition", 42UL, ["timed out"], string.Empty, FailureKind.Timeout, failure.Id, failure.Deferral!.Id);
        }
    }

    [Fact] void should_park_after_the_initial_delivery_and_the_retry_budget() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_consume_one_attempt_per_delivery() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(3);
}
