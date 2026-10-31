// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_retry_after_seconds : given.a_webhook_mediator
{
    WebhookTarget _target;
    FakeHttpMessageHandler _handler;
    HttpClient _client;
    WebhookDeliveryResult _result;
    DateTimeOffset _before;

    void Establish()
    {
        _target = new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>());
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("Retry-After", "120");
        _handler = new(response);
        _client = new(_handler) { BaseAddress = new Uri("https://example.com/webhook") };
        _httpClientFactory.Create(_target).Returns(_client);
    }

    async Task Because()
    {
        _before = DateTimeOffset.UtcNow;
        _result = await _mediator.Deliver("orders", _target, "partition", []);
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_send_only_one_request() => _handler.RequestsSent.ShouldEqual(1);
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_defer_without_a_consumed_failure() => _result.Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RetryAfter);
    [Fact] void should_keep_the_receiver_deadline() => ((_result.Deferral!.Until - _before).TotalSeconds >= 119d && (_result.Deferral!.Until - _before).TotalSeconds <= 122d).ShouldBeTrue();
}
