// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class with_a_timed_out_request_and_sensitive_target : given.a_webhook_mediator_with_recorded_logs
{
    WebhookDeliveryResult _result;

    void Establish() => _httpClientFactory.Create(_target).Returns(new HttpClient(new FakeHttpMessageHandler(new TaskCanceledException(SensitiveTarget.Reason))) { BaseAddress = new Uri(_target.Url) });

    async Task Because() => _result = await _mediator.Deliver("orders", _target, "partition", []);

    [Fact] void should_keep_retry_policy() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.Retry);
    [Fact] void should_not_defer() => _result.Deferral.ShouldBeNull();
    [Fact] void should_preserve_timeout_error() => _result.Error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_invent_a_timeout_log() => _logs.Entries.ShouldBeEmpty();
    [Fact] void should_not_log_sensitive_content() => _logs.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
