// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

/// <summary>
/// Quarantining stops retries and needs an operator to undo, which is the right answer for an observer that is wrong
/// and the wrong answer for one that is only waiting on a congested kernel. A busy period must not be able to take a
/// healthy projection out of service and leave the operator who clears it finding nothing wrong.
/// </summary>
public class and_the_failures_are_timeouts : given.an_observer_with_subscription
{
    void Establish() => _configurationProvider.GetFor(Arg.Any<string>()).Returns(new Observers { QuarantineOnFailedPartitionCount = 1 });

    async Task Because() => await _observer.PartitionFailed("partition-1", 42UL, ["Response did not arrive on time"], "stacktrace", FailureKind.Timeout);

    [Fact] void should_not_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_still_record_the_partition_as_failed() => _failedPartitionsStorage.State.IsFailed("partition-1").ShouldBeTrue();
    [Fact] void should_record_what_kind_of_failure_it_was() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.Kind.ShouldEqual(FailureKind.Timeout);
}
