// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_an_excessive_retry_after_date : given.a_webhook_mediator
{
    WebhookTarget _target;
    FakeHttpMessageHandler _handler;
    HttpClient _client;
    WebhookDeliveryResult _result;
    DateTimeOffset _before;
    DateTimeOffset _after;

    void Establish()
    {
        _target = new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        response.Headers.TryAddWithoutValidation("Retry-After", DateTimeOffset.UtcNow.AddDays(10).ToString("r"));
        _handler = new(response);
        _client = new(_handler) { BaseAddress = new Uri("https://example.com/webhook") };
        _httpClientFactory.Create(_target).Returns(_client);
    }

    async Task Because()
    {
        _before = DateTimeOffset.UtcNow;
        _result = await _mediator.Deliver("orders", _target, "partition", []);
        _after = DateTimeOffset.UtcNow;
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_send_only_one_request() => _handler.RequestsSent.ShouldEqual(1);
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_defer_the_service_unavailable_response() => _result.Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RetryAfter);
    [Fact] void should_clamp_the_deadline_to_the_default_maximum() => (_result.Deferral!.Until >= _before.AddSeconds(86400) && _result.Deferral!.Until <= _after.AddSeconds(86400)).ShouldBeTrue();
}
