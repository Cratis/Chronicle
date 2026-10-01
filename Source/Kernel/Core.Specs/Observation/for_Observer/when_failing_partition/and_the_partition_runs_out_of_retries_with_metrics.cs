// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_partition_runs_out_of_retries_with_metrics : given.an_observer_with_metrics
{
    void Establish() => _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { MaxRetryAttempts = 1 });

    async Task Because()
    {
        await _observer.PartitionFailed("Something", 42UL, ["Something went wrong"], "This is the stack trace");
        await _observer.PartitionFailed("Something", 42UL, ["Something went wrong"], "This is the stack trace");
    }

    [Fact] void should_count_both_failed_attempts() => _metrics.SumOf(PartitionsFailed).ShouldEqual(2);
    [Fact] void should_count_the_partition_as_quarantined_once() => _metrics.SumOf(PartitionsQuarantined).ShouldEqual(1);
    [Fact] void should_not_count_a_quarantined_observer() => _metrics.For(ObserverQuarantined).ShouldBeEmpty();
    [Fact] void should_not_tag_the_quarantined_partition_with_the_partition() => IsNotTaggedWithPartition(PartitionsQuarantined).ShouldBeTrue();
    [Fact] void should_tag_the_quarantined_partition_with_the_observer_scope_only() => IsTaggedWithObserverScopeOnly(PartitionsQuarantined).ShouldBeTrue();
}
