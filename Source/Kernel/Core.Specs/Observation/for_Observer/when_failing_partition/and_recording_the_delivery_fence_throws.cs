// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_recording_the_delivery_fence_throws : given.an_observer_with_metrics
{
    readonly InvalidOperationException _failure = new("Fence configuration unavailable");
    Exception _error;

    void Establish()
    {
        var calls = 0;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_ => ++calls == 1 ? _observersConfig : throw _failure);
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.PartitionFailedWithDeliveryFence(
        "partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout,
        context: new(DeliveryFence: new(DateTimeOffset.UtcNow.AddMinutes(5), []))));

    [Fact] void should_propagate_the_fence_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_count_the_partition_failure() => _metrics.SumOf(PartitionsFailed).ShouldEqual(1);
    [Fact] void should_not_count_a_legacy_retry_attempt() => _metrics.SumOf(PartitionRetryAttempts).ShouldEqual(0);
    [Fact] void should_not_count_a_quarantined_partition() => _metrics.SumOf(PartitionsQuarantined).ShouldEqual(0);
}
