// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_WebhookReactor;

public class when_removing_webhook : given.a_webhook_reactor
{
    WebhookRemoved _event;
    EventContext _eventContext;
    WebhookId _webhookId;

    void Establish()
    {
        _webhookId = new WebhookId(Guid.NewGuid().ToString());
        _event = new WebhookRemoved();
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

    [Fact] void should_remove_webhook_from_manager() =>
        _webhooksManager.Received(1).Remove(Arg.Is<WebhookId>(id => id.Value == _webhookId.Value));
}
