// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_successful_delivery_to_a_sensitive_target : given.a_webhook_delivery_to_a_sensitive_target
{
    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_succeed() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_advance_to_last_event() => _deliveryResult.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)44);
    [Fact] void should_send_one_payload() => _receiver.Payloads.Count.ShouldEqual(1);
    [Fact] void should_log_a_success_trace() => _logger.Entries.Any(entry => entry.Level == LogLevel.Trace && entry.Message.Contains("Successfully", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_log_sensitive_content() => _logger.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
