// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_policy_read_failure_echoing_sensitive_content : given.a_compatibility_subscription_with_a_pinned_definition
{
    void Establish() => _webhookDefinitions.Get("webhook").Returns(Task.FromException<WebhookDefinition>(new IOException(SensitiveTarget.Reason)));

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_fail_the_batch() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_send() => _receiver.Payloads.ShouldBeEmpty();
    [Fact] void should_not_log_sensitive_content() => _logger.Entries.Any(entry => SensitiveTarget.Leaks(entry.Text)).ShouldBeFalse();
}
