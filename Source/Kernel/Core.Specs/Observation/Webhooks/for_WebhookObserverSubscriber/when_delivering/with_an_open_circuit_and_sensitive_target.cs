// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks.given;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_an_open_circuit_and_sensitive_target : given.a_webhook_delivery_to_a_sensitive_target
{
    void Establish() => _receiver.Failure = new BrokenCircuitException(SensitiveTarget.Reason);

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_defer_the_partition() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_keep_circuit_reason() => _deliveryResult.Deferral.Reason.ShouldEqual(PartitionDeferralReason.CircuitOpen);
    [Fact] void should_not_acknowledge_events() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_sensitive_messages() => _deliveryResult.ExceptionMessages.Any(SensitiveTarget.Leaks).ShouldBeFalse();
    [Fact] void should_omit_raw_exception_stack() => _deliveryResult.ExceptionStackTrace.ShouldEqual(string.Empty);
    [Fact] void should_not_log_sensitive_content() => _logger.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
