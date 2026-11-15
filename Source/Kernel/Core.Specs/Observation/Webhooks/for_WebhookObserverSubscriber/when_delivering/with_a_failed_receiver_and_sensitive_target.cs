// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_failed_receiver_and_sensitive_target : given.a_webhook_delivery_to_a_sensitive_target
{
    void Establish()
    {
        _receiver.StatusCode = HttpStatusCode.InternalServerError;
        _receiver.ReasonPhrase = SensitiveTarget.Reason;
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_fail_the_batch() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_events() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_persist_sensitive_messages() => _deliveryResult.ExceptionMessages.Any(SensitiveTarget.Leaks).ShouldBeFalse();
    [Fact] void should_omit_raw_exception_stack() => _deliveryResult.ExceptionStackTrace.ShouldEqual(string.Empty);
    [Fact] void should_record_failure() => _logger.Entries.Any(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ShouldBeTrue();
    [Fact] void should_not_log_sensitive_content() => _logger.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
