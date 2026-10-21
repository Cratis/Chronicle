// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_gone_interrupts_a_successful_probe_checkpoint : given.a_webhook_ready_to_probe
{
    async Task Establish()
    {
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>())
            .Returns(Task.FromException(new InvalidOperationException("Local storage unavailable")));
        await Tick();
        await _observer.PartitionFailed("another-partition", 60UL, ["Gone"], string.Empty, failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt: new WebhookDeliveryAttempt(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition)));
    }

    async Task Because()
    {
        await Crash();
        _observerHandledCountsStorage.SetForRecovery(_observerId, "partition", Arg.Any<IReadOnlyDictionary<EventTypeId, EventCount>>()).Returns(Task.CompletedTask);
        await _observer.RunWatchdogAsync();
        await Crash();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_keep_the_successful_delivery_cursor() => _failedPartitionsState.Partitions.Single(partition => partition.Partition == "partition").EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_count_the_success_once() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)1UL);
    [Fact] void should_keep_the_terminal_reason() => _stateStorage.State.WebhookRetirement.ShouldNotBeNull();
    [Fact] void should_finish_the_local_checkpoint() => _stateStorage.State.WebhookProbe.ShouldBeNull();
}
