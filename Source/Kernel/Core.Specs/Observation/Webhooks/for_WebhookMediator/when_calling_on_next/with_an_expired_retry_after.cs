// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_expired_retry_after : given.a_webhook_mediator
{
    WebhookTarget _target;
    FakeHttpMessageHandler _handler;
    HttpClient _client;
    WebhookDeliveryResult _result;

    void Establish()
    {
        _target = new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryAddWithoutValidation("Retry-After", DateTimeOffset.UtcNow.AddMinutes(-5).ToString("r"));
        _handler = new(response);
        _client = new(_handler) { BaseAddress = new Uri("https://example.com/webhook") };
        _httpClientFactory.Create(_target).Returns(_client);
    }

    async Task Because()
    {
        _result = await _mediator.Deliver("orders", _target, "partition", []);
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_send_only_one_request() => _handler.RequestsSent.ShouldEqual(1);
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_fall_back_to_the_failure_budget() => _result.Deferral.ShouldBeNull();
    [Fact] void should_classify_it_as_retryable() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
}
