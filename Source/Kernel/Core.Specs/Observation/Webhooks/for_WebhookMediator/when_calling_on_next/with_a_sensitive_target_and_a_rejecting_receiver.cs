// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_sensitive_target_and_a_rejecting_receiver : given.a_webhook_mediator
{
    WebhookTarget _target;
    FakeHttpMessageHandler _handler;
    WebhookDeliveryResult _result;

    void Establish()
    {
        _target = new(SensitiveTarget.WithCredentials(SensitiveTarget.UniqueHost()), WebhookAuthorization.None, new Dictionary<string, string>());
        _handler = new(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = SensitiveTarget.Reason });
        _httpClientFactory.Create(_target).Returns(new HttpClient(_handler) { BaseAddress = new Uri(_target.Url) });
    }

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_send_once() => _handler.RequestsSent.ShouldEqual(1);
    [Fact] void should_preserve_request_userinfo() => _handler.LastRequestUri.UserInfo.ShouldContain(SensitiveTarget.User);
    [Fact] void should_preserve_request_password() => _handler.LastRequestUri.UserInfo.ShouldContain(SensitiveTarget.Password);
    [Fact] void should_preserve_request_path() => _handler.LastRequestUri.AbsolutePath.ShouldContain(SensitiveTarget.PathMarker);
    [Fact] void should_preserve_request_query() => _handler.LastRequestUri.Query.ShouldContain(SensitiveTarget.QueryMarker);
    [Fact] void should_return_delivery_failure() => _result.Error.ShouldBeOfExactType<WebhookDeliveryFailed>();
    [Fact] void should_preserve_default_failure_policy() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
}
