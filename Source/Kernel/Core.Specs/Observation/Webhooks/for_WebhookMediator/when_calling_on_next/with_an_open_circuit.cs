// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_open_circuit : given.a_webhook_mediator
{
    WebhookDeliveryResult _result;
    DateTimeOffset _before;
    FakeHttpMessageHandler _handler;
    WebhookTarget _target;

    void Establish()
    {
        _target = new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>());
        _handler = new(new BrokenCircuitException(TimeSpan.FromMinutes(2)));
        _httpClientFactory.Create(_target).Returns(new HttpClient(_handler) { BaseAddress = new Uri(_target.Url) });
    }

    async Task Because()
    {
        _before = DateTimeOffset.UtcNow;
        _result = await _mediator.Deliver("orders", _target, "partition", [AppendedEvent.Empty() with { Context = EventContext.Empty with { SequenceNumber = new(42) } }]);
    }

    [Fact] void should_return_a_typed_deferral() => _result.Deferral.Reason.ShouldEqual(PartitionDeferralReason.CircuitOpen);
    [Fact] void should_resume_at_the_first_unhandled_event() => _result.Deferral.ResumeFrom.ShouldEqual(new EventSequenceNumber(42));
    [Fact] void should_use_the_breaker_deadline() => (_result.Deferral.Until >= _before.AddMinutes(2)).ShouldBeTrue();
    [Fact] void should_not_claim_an_event_was_handled() => _result.LastHandled.ShouldEqual(EventSequenceNumber.Unavailable);
}
