// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_for_an_event_store_subscription : given.an_observer
{
    async Task Establish()
    {
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookTargetValidation(false, "Webhook target must use HTTPS."));
        await _observer.Subscribe<IEventStoreSubscriptionObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);
        await _observer.TransitionTo<QuarantinedObserver>();
        _webhookTargetPolicy.ClearReceivedCalls();
    }

    Task Because() => _observer.ClearObserverQuarantine();

    [Fact] void should_not_evaluate_webhook_policy() => _webhookTargetPolicy.DidNotReceive().EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_read_webhook_definitions() => _eventStoreStorage.Webhooks.DidNotReceive().Get(Arg.Any<Concepts.Observation.Webhooks.WebhookId>());
    [Fact] async Task should_leave_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
}
