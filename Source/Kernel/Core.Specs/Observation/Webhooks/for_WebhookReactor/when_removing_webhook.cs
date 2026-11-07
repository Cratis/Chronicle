// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_removing_webhook : given.a_webhook_reactor
{
    WebhookRemoved _event;
    EventContext _eventContext;
    WebhookId _webhookId;
    readonly List<string> _operations = [];

    void Establish()
    {
        _webhookId = new WebhookId(Guid.NewGuid().ToString());
        _event = new WebhookRemoved();
        _webhooksManager.When(manager => manager.Remove(_webhookId)).Do(_ => _operations.Add("remove"));
        _signingKeyRing.When(ring => ring.RetireWebhookSecrets(_webhookId)).Do(_ => _operations.Add("retire"));
        _eventContext = EventContext.From(
            new EventStoreName("test-store"),
            EventStoreNamespaceName.Default,
            EventType.Unknown,
            EventSourceType.Default,
            new EventSourceId(_webhookId.Value),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            new CorrelationId(Guid.NewGuid()));
    }

    async Task Because() => await _reactor.Removed(_event, _eventContext);

    [Fact] void should_retire_the_webhook_secrets() => _signingKeyRing.Received(1).RetireWebhookSecrets(_webhookId);

    [Fact] void should_use_the_event_store_key() => _grainFactory.Received(1).GetGrain<IWebhookSigningKeyRing>(_eventContext.EventStore.Value);

    [Fact] void should_retire_after_removing() => _operations.ShouldEqual(["remove", "retire"]);

    [Fact] void should_remove_webhook_from_manager() =>
        _webhooksManager.Received(1).Remove(Arg.Is<WebhookId>(id => id.Value == _webhookId.Value));
}
