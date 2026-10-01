// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_quarantining_observer;

public class and_failed_partitions_reach_the_threshold_with_metrics : given.an_observer_with_metrics
{
    void Establish() => _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });

    async Task Because()
    {
        await _observer.PartitionFailed("partition-1", 42UL, ["something failed"], "stacktrace");

        // A further failure while quarantined is not another quarantine.
        await _observer.PartitionFailed("partition-2", 43UL, ["something failed"], "stacktrace");
    }

    [Fact] void should_count_the_observer_as_quarantined_once() => _metrics.SumOf(ObserverQuarantined).ShouldEqual(1);
    [Fact] void should_not_tag_the_quarantine_with_a_partition() => IsNotTaggedWithPartition(ObserverQuarantined).ShouldBeTrue();
    [Fact] void should_tag_the_quarantine_with_the_observer_scope_only() => IsTaggedWithObserverScopeOnly(ObserverQuarantined).ShouldBeTrue();
}
