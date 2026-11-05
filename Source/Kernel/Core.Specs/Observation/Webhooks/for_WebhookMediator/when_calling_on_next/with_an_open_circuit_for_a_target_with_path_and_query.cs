// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;
using Polly.CircuitBreaker;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_open_circuit_for_a_target_with_path_and_query : given.a_webhook_mediator
{
    readonly string _host = SensitiveTarget.UniqueHost();
    readonly WebhookMetricsRecorder _metrics = new();
    WebhookTarget _target;
    WebhookDeliveryResult _result;

    void Establish()
    {
        _target = new(SensitiveTarget.WithPathAndQuery(_host), WebhookAuthorization.None, new Dictionary<string, string>());
        _httpClientFactory.Create(_target).Returns(new HttpClient(new FakeHttpMessageHandler(new BrokenCircuitException())) { BaseAddress = new Uri(_target.Url) });
    }

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_record_one_rejection() => _metrics.ForHost(_host).Single().Instrument.ShouldEqual("chronicle-webhook-circuit-rejected");
    [Fact] void should_keep_the_authority_series() => _metrics.ForHost(_host).Single().Tags["authority"].ShouldEqual($"https://{_host}:8443");
    [Fact] void should_defer_for_the_circuit() => _result.Deferral.Reason.ShouldEqual(PartitionDeferralReason.CircuitOpen);

    void Destroy() => _metrics.Dispose();
}
