// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_gone_retired_webhook : a_webhook_ready_to_probe
{
    protected WebhookDeliveryAttempt _goneAttempt;
    protected Guid _retirementId;
    protected Guid _registrationId = Guid.NewGuid();

    async Task Establish()
    {
        _webhookDefinition = _webhookDefinition with { Target = _webhookDefinition.Target with { Url = "https://user:secret@receiver.example:8443/private?token=secret#fragment" } };
        _webhookDefinitions.Get(_observerId).Returns(_webhookDefinition);
        _goneAttempt = new(_stateStorage.State.AlertLifecycleId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition));
        await _observer.PartitionFailed("partition", 42UL, ["unsafe receiver detail"], "", failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt: _goneAttempt);
        await _observer.RunWatchdogAsync();
        _retirementId = _stateStorage.State.WebhookRetirement.Id;
    }

    protected async Task RegisterAgain()
    {
        await _observer.AcceptWebhookRegistration(_registrationId, WebhookDeliveryAttempt.Fingerprint(_webhookDefinition));
        await _observer.ReconcileWebhookRegistration();
    }
}
