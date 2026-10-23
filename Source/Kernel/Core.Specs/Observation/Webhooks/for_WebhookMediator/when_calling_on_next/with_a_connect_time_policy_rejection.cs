// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_connect_time_policy_rejection : given.a_webhook_mediator
{
    WebhookTarget _target;
    WebhookDeliveryResult _result;
    readonly WebhookTargetRejected _rejection = new("Webhook target resolves to a private or special address.");

    void Establish()
    {
        _target = new("https://receiver.example/hook", WebhookAuthorization.None, new Dictionary<string, string>());
        _httpClientFactory.Create(_target).Returns(_ => throw new HttpRequestException("Connection failed", _rejection));
    }

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_pause_the_observer() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.PauseObserver);
    [Fact] void should_report_only_the_safe_policy_reason() => _result.Error.ShouldEqual(_rejection);
    [Fact] void should_not_defer_a_transient_retry() => _result.Deferral.ShouldBeNull();
}
