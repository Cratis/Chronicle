// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Properties;
using Catch = Cratis.Monads.Catch;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_client_error_response : given.a_webhook_mediator
{
    WebhookTarget _target;
    Catch _result;

    void Establish()
    {
        _target = new WebhookTarget(
            new WebhookTargetUrl("https://example.com/webhook"),
            WebhookAuthorization.None,
            new Dictionary<string, string>());

        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Invalid webhook request",
            Content = new StringContent("sensitive response body")
        };
        var httpClient = new HttpClient(new FakeHttpMessageHandler(response))
        {
            BaseAddress = new Uri("https://example.com/webhook")
        };
        _httpClientFactory.Create(_target).Returns(httpClient);
    }

    async Task Because() => _result = await _mediator.OnNext(_target, new Key("test-partition", ArrayIndexers.NoIndexers), []);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_status_and_webhook_target()
    {
        _result.TryGetException(out var exception).ShouldBeTrue();
        exception.ShouldBeOfExactType<WebhookDeliveryFailed>();
        var failure = (WebhookDeliveryFailed)exception;
        failure.StatusCode.ShouldEqual(HttpStatusCode.BadRequest);
        failure.ReasonPhrase.ShouldEqual("Invalid webhook request");
        failure.TargetUrl.ShouldEqual(_target.Url);
    }
}
