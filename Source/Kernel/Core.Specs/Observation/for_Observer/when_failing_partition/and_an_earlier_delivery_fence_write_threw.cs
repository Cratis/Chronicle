// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_an_earlier_delivery_fence_write_threw : given.an_observer
{
    readonly InvalidOperationException _failure = new("Fence configuration unavailable");
    Exception _error;

    async Task Establish()
    {
        var calls = 0;
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_ => ++calls == 1 ? _observersConfig : throw _failure);
        _error = await Catch.Exception(() => _observer.PartitionFailedWithDeliveryFence(
            "first",
            42UL,
            ["timeout"],
            string.Empty,
            FailureKind.Timeout,
            context: new(DeliveryFence: new(DateTimeOffset.UtcNow.AddMinutes(5), []))));
        _configurationProvider.Configure().GetFor(Arg.Any<string>()).Returns(_observersConfig);
    }

    async Task Because() => await _observer.PartitionFailed("next", 43UL, ["Failure"], "Stack").WaitAsync(TimeSpan.FromSeconds(5), _timeProvider);

    [Fact] void should_have_propagated_the_original_exception() => _error.ShouldBeSame(_failure);
    [Fact] void should_release_the_mutation_lock_for_the_next_failure() => _failedPartitionsState.Partitions.Any(_ => _.Partition == "next").ShouldBeTrue();
    [Fact] void should_persist_the_next_failure() => _failedPartitionsStorageStats.Writes.ShouldEqual(1);
}
