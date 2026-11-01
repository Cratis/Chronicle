// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_sending_an_envelope : given.a_delivery_request
{
    given.recording_http_handler _handler;
    WebhookDeliveryResult _result;
    string _traceParent;
    DateTimeOffset _deadline;

    async Task Because()
    {
        _handler = new();
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(new HttpClient(_handler) { BaseAddress = new("https://example.com/webhook") });
        var mediator = new WebhookMediator(factory, _options);
        using var activity = new Activity("spec-delivery").SetIdFormat(ActivityIdFormat.W3C).Start();
        _traceParent = activity.Id!;
        _deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        _result = await mediator.DeliverEnvelope(_request with { DeliverBy = _deadline });
        await mediator.DeliverEnvelope(_request with { DeliverBy = _deadline.AddSeconds(10) });
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_last_delivered_sequence() => _result.LastHandled.Value.ShouldEqual(42UL);
    [Fact] void should_send_versioned_media_type() => _handler.MediaTypes[0].ShouldEqual(DeliveryMediaType.Value);
    [Fact] void should_set_one_traceparent() => _handler.Headers[0][DeliveryHeaders.TraceParent].ShouldEqual(_traceParent);
    [Fact] void should_set_utc_deadline() => _handler.Headers[0][DeliveryHeaders.Deadline].ShouldEqual(_deadline.ToString("O"));
    [Fact] void should_match_header_and_body_identity() => _handler.Headers[0][DeliveryHeaders.Id].ShouldEqual(JsonDocument.Parse(_handler.Bodies[0]).RootElement.GetProperty("deliveryId").GetString());
    [Fact] void should_preserve_bytes_across_attempt_metadata_changes() => Convert.ToHexString(_handler.Bodies[0]).ShouldEqual(Convert.ToHexString(_handler.Bodies[1]));
    [Fact] void should_keep_delivery_id_on_retry() => _handler.Headers[0][DeliveryHeaders.Id].ShouldEqual(_handler.Headers[1][DeliveryHeaders.Id]);
    [Fact] void should_not_pretend_to_sign() => _handler.Headers[0].ContainsKey("webhook-signature").ShouldBeFalse();
}
