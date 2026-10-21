// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_old_gone_result_arrives_after_registration : given.a_gone_retired_webhook
{
    async Task Because()
    {
        await RegisterAgain();
        await _observer.PartitionFailed("partition", 42UL, ["410"], "", failurePolicy: WebhookDeliveryFailurePolicy.RetireWebhook, deliveryAttempt: _goneAttempt);
    }

    [Fact] void should_not_retire_the_new_registration() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_not_restore_the_old_reason() => _stateStorage.State.WebhookRetirement.ShouldBeNull();
}
