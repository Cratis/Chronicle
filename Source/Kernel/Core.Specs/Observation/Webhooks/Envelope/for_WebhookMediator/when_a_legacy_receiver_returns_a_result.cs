// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_a_legacy_receiver_returns_a_result : given.a_delivery_request
{
    given.recording_http_handler _handler;
    WebhookDeliveryResult _result;

    void Establish() => _handler = new()
    {
        ResponseBody = JsonSerializer.Serialize(new
        {
            results = new[] { new { partition = _request.Partition.ToString(), state = "failed", lastSuccessfulObservation = (ulong?)null } }
        })
    };

    async Task Because()
    {
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(new HttpClient(_handler) { BaseAddress = new("https://example.com/webhook") });
        _result = await new WebhookMediator(factory, _options).Deliver(_request.EventStore, _request.Target, _request.Partition, _request.Events);
    }

    [Fact] void should_preserve_legacy_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_last_event() => _result.LastHandled.Value.ShouldEqual(42UL);
    [Fact] void should_have_no_error() => _result.Error.ShouldBeNull();
}
