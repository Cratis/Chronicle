// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_stored_webhook_violates_warn_policy : given.an_observer
{
    readonly given.RecordingObserverLogger _recordedLogs = new();

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    void Establish() => _webhookTargetPolicy.EvaluateForDelivery(Arg.Any<string>(), Arg.Any<EventStoreName?>(), Arg.Any<CancellationToken>())
        .Returns(new WebhookTargetValidation(true, "Webhook target must use HTTPS."));

    Task Because() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);

    [Fact] void should_log_the_safe_warning_once() => _recordedLogs.Warnings.Count(message => message.Contains("Webhook target must use HTTPS.", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_evaluate_the_stored_target() => _webhookTargetPolicy.Received(1).EvaluateForDelivery("https://receiver.example/hook", _observerKey.EventStore, Arg.Any<CancellationToken>());
    [Fact] void should_continue_live_delivery() => _appendedEventsQueues.Received().Subscribe(_observerKey, Arg.Any<IEnumerable<Concepts.Events.EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_not_quarantine_the_observer() => _stateStorage.State.RunningState.ShouldNotEqual(ObserverRunningState.Quarantined);
}
