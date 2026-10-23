// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_partition_quarantines_a_webhook_with_an_eligible_failure : given.a_webhook_observer
{
    ObserverRunningState _stateBeforeGone;

    protected override Configuration.Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, QuarantineOnFailedPartitionCount = 1, QuarantineOnFailedPartitionPercentage = 0 };

    async Task Establish()
    {
        await _observer.PartitionFailed("eligible", 41UL, ["500"], "", FailureKind.Handling);
        _stateBeforeGone = _stateStorage.State.RunningState;
    }

    Task Because() => _observer.PartitionFailed("gone", 42UL, ["410"], "", FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.ParkPartition);

    [Fact] void should_be_active_before_the_gone_failure() => _stateBeforeGone.ShouldEqual(ObserverRunningState.Active);
    [Fact] void should_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_arm_the_probe_for_the_eligible_partition() => _stateStorage.State.WebhookProbe.ShouldNotBeNull();
}
