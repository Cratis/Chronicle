// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_clearing_quarantine_after_fixing_a_stored_webhook : given.an_observer
{
    async Task Establish()
    {
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
            .Returns(new WebhookTargetValidation(false, "Webhook target must use HTTPS."));
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);
        _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>()).Returns(WebhookTargetValidation.Allowed);
        _webhookTargetPolicy.ClearReceivedCalls();
    }

    Task Because() => _observer.ClearObserverQuarantine();

    [Fact] void should_reevaluate_current_policy() => _webhookTargetPolicy.Received(1).EvaluateForDelivery("https://receiver.example/hook", _observerKey.EventStore, Arg.Any<CancellationToken>());
    [Fact] void should_resume_delivery() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_consume_a_partition_retry_budget() => _failedPartitionsState.Partitions.ShouldBeEmpty();
}
