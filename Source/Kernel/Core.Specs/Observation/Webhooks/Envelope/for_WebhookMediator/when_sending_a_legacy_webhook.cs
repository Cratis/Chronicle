// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_sending_a_legacy_webhook : given.a_delivery_request
{
    given.recording_http_handler _handler;
    WebhookDeliveryResult _result;
    byte[] _expected;

    async Task Because()
    {
        _handler = new();
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(new HttpClient(_handler) { BaseAddress = new("https://example.com/webhook") });
        _expected = JsonSerializer.SerializeToUtf8Bytes(new EventsToObserve(_request.Partition.ToString(), _request.Events), _options);
        _result = await new WebhookMediator(factory, _options).Deliver(_request.EventStore, _request.Target, _request.Partition, _request.Events);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_body_bytes() => Convert.ToHexString(_handler.Bodies[0]).ShouldEqual(Convert.ToHexString(_expected));
    [Fact] void should_preserve_the_content_type() => _handler.MediaTypes[0].ShouldEqual("application/json; charset=utf-8");
    [Fact] void should_not_add_delivery_identity() => _handler.Headers[0].ContainsKey(DeliveryHeaders.Id).ShouldBeFalse();
    [Fact] void should_not_add_a_deadline() => _handler.Headers[0].ContainsKey(DeliveryHeaders.Deadline).ShouldBeFalse();
    [Fact] void should_not_add_trace_context() => _handler.Headers[0].ContainsKey(DeliveryHeaders.TraceParent).ShouldBeFalse();
}
