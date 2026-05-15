// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_failed_partition_count_reaches_observer_quarantine_threshold : given.an_observer_with_subscription
{
    void Establish() => _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });

    async Task Because() => await _observer.PartitionFailed("partition-1", 42UL, ["something failed"], "stacktrace");

    [Fact] void should_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
