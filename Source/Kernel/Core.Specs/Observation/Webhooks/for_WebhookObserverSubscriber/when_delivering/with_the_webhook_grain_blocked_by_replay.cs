// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_the_webhook_grain_blocked_by_replay : given.a_webhook_delivery
{
    readonly TaskCompletionSource<WebhookDefinition> _blockedDefinition = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _webhook.GetDefinition().Returns(_blockedDefinition.Task);

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null)).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

    [Fact] void should_deliver_without_waiting_for_the_webhook_grain() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned"));
    [Fact] async Task should_not_call_back_to_the_replaying_webhook() => await _webhook.DidNotReceive().GetDefinition();
    [Fact] async Task should_resolve_the_committed_policy_once_per_batch() => await _webhookDefinitions.Received(1).Get("webhook");
    [Fact] void should_acknowledge_the_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
