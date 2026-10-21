// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_gone_receiver : given.a_webhook_mediator
{
    WebhookTarget _target;
    FakeHttpMessageHandler _handler;
    HttpClient _client;
    WebhookDeliveryResult _result;

    void Establish()
    {
        _target = new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        var response = new HttpResponseMessage(HttpStatusCode.Gone);
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
    [Fact] void should_retire_without_automatic_retries() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.RetireWebhook);
    [Fact] void should_preserve_the_numeric_status() => _result.HttpStatusCode.ShouldEqual(410);
    [Fact] void should_not_schedule_a_deadline() => _result.Deferral.ShouldBeNull();
}
