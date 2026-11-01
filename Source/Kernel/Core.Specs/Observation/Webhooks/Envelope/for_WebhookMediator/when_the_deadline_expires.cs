// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_WebhookMediator;

public class when_the_deadline_expires : given.a_delivery_request
{
    given.recording_http_handler _handler;
    WebhookDeliveryResult _result;

    async Task Because()
    {
        _handler = new() { WaitForCancellation = true };
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(_request.Target, _request.AccessToken).Returns(new HttpClient(_handler) { BaseAddress = new("https://example.com/webhook") });
        _result = await new WebhookMediator(factory, _options).DeliverEnvelope(_request with { DeliverBy = DateTimeOffset.UtcNow.AddMilliseconds(250) });
    }

    [Fact] void should_cancel_the_request() => _handler.WasCancelled.ShouldBeTrue();
    [Fact] void should_classify_as_a_failed_delivery() => _result.IsSuccess.ShouldBeFalse();
}
