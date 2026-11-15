// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks.given;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_open_circuit_and_sensitive_target : given.a_webhook_mediator_with_recorded_logs
{
    WebhookDeliveryResult _result;

    void Establish() => _httpClientFactory.Create(_target).Returns(new HttpClient(new FakeHttpMessageHandler(new BrokenCircuitException(SensitiveTarget.Reason))) { BaseAddress = new Uri(_target.Url) });

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_defer_for_open_circuit() => _result.Deferral.Reason.ShouldEqual(PartitionDeferralReason.CircuitOpen);
    [Fact] void should_record_target_unavailability() => _logs.Entries.Count().ShouldEqual(1);
    [Fact] void should_not_log_sensitive_content() => _logs.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
