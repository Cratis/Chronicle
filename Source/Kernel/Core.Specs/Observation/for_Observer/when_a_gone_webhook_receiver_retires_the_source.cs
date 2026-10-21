// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_receiver_retires_the_source : given.a_webhook_with_a_definition
{
    Task Because() => _observer.PartitionFailed("partition", 42UL, ["Gone"], string.Empty, FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt:
        new WebhookDeliveryAttempt(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition)));

    [Fact] void should_retire_the_entire_source() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_remember_the_terminal_reason() => _stateStorage.State.WebhookRetirement.ShouldNotBeNull();
    [Fact] void should_preserve_the_failed_event() => _failedPartitionsState.Partitions.Single().EffectiveRecoveryCursor.Position.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_record_the_gone_failure() => _failedPartitionsState.Partitions.Single().WebhookDisposition.ShouldEqual(WebhookFailureDisposition.Gone);
    [Fact] void should_not_record_recoverable_quarantine() => _stateStorage.State.QuarantineEpisodeId.ShouldBeNull();
}
