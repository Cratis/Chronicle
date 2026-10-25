// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_dns_is_temporarily_unavailable_in_warn_mode : given.a_stored_webhook_with_unavailable_dns
{
    readonly given.RecordingObserverLogger _recordedLogs = new();

    protected override WebhookValidationMode ValidationMode => WebhookValidationMode.Warn;

    protected override ILogger<Observer> CreateLogger() => _recordedLogs;

    Task Because() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1"], SiloAddress.Zero);

    [Fact] async Task should_subscribe_despite_the_dns_warning() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] void should_log_the_temporary_dns_warning_once() => _recordedLogs.Warnings.Count(message => message.Contains("Webhook target host resolution is temporarily unavailable.", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_continue_live_delivery() => _appendedEventsQueues.Received().Subscribe(_observerKey, Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
