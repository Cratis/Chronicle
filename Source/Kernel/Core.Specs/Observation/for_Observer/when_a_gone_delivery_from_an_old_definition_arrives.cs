// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_delivery_from_an_old_definition_arrives : given.a_webhook_with_a_definition
{
    WebhookDeliveryAttempt _attempt;

    void Establish()
    {
        _attempt = new(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition));
        _webhookDefinition = _webhookDefinition with { Target = _webhookDefinition.Target with { Url = "https://replacement.test/events" } };
    }

    Task Because() => _observer.PartitionFailed("partition", 42UL, ["Gone"], string.Empty, FailureKind.Handling, failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt: _attempt);

    [Fact] void should_not_retire_the_current_source() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_not_create_a_terminal_reason() => _stateStorage.State.WebhookRetirement.ShouldBeNull();
    [Fact] void should_not_register_a_failure_for_the_new_definition() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
