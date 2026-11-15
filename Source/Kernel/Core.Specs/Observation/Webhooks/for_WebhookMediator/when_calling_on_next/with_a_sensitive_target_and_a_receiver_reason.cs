// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_sensitive_target_and_a_receiver_reason : given.a_webhook_mediator
{
    WebhookTarget _target;
    WebhookDeliveryResult _result;

    void Establish()
    {
        _target = new(SensitiveTarget.WithCredentials(SensitiveTarget.UniqueHost()), WebhookAuthorization.None, new Dictionary<string, string>());
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError) { ReasonPhrase = SensitiveTarget.Reason });
        _httpClientFactory.Create(_target).Returns(new HttpClient(handler) { BaseAddress = new Uri(_target.Url) });
    }

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_retain_safe_authority() => ((WebhookDeliveryFailed)_result.Error).TargetUrl.ShouldEqual((WebhookTargetUrl)WebhookDiagnostics.Authority(_target.Url));
    [Fact] void should_omit_receiver_reason() => ((WebhookDeliveryFailed)_result.Error).ReasonPhrase.ShouldBeNull();
    [Fact] void should_retain_status() => ((WebhookDeliveryFailed)_result.Error).StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
}
