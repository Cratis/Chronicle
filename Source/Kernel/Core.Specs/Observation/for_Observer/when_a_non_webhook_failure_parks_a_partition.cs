// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Moq;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_non_webhook_failure_parks_a_partition : given.an_observer
{
    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionCount = 1 };

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Generation unavailable"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_park_immediately() => _failedPartitionsState.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_not_quarantine_the_observer_at_its_threshold() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_defer_the_partition() => _failedPartitionsState.Partitions.Single().Deferral.ShouldBeNull();
    [Fact] void should_not_register_a_retry() => _silo.ReminderRegistry.Mock.Verify(_ => _.RegisterOrUpdateReminder(It.IsAny<GrainId>(), It.Is<string>(name => name.StartsWith("cp:", StringComparison.Ordinal)), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()), Times.Never);
}
