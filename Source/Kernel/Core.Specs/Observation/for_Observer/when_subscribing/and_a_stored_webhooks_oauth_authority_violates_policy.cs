// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_stored_webhooks_oauth_authority_violates_policy : given.an_observer
{
    async Task Establish()
    {
        var definition = await _eventStoreStorage.Webhooks.Get((WebhookId)_observerId.Value);
        _eventStoreStorage.Webhooks.Get((WebhookId)_observerId.Value).Returns(definition with
        {
            Target = definition.Target with { Authorization = new OAuthAuthorization("http://identity.example", "client", "secret") }
        });
        _webhookTargetPolicy.EvaluateForDelivery("http://identity.example", Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookTargetValidation(false, "Webhook target must use HTTPS."));
    }

    Task Because() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);

    [Fact] void should_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_consume_a_partition_retry_budget() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
