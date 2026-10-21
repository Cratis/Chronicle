// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_gone_webhook_has_only_a_pending_registration : given.a_gone_retired_webhook
{
    async Task Because()
    {
        await _observer.AcceptWebhookRegistration(_registrationId, "not-the-current-definition");
        await _observer.ReconcileWebhookRegistration();
    }

    [Fact] void should_remain_retired_until_the_accepted_definition_is_durable() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_keep_the_incident() => _stateStorage.State.WebhookRetirement.Id.ShouldEqual(_retirementId);
}
