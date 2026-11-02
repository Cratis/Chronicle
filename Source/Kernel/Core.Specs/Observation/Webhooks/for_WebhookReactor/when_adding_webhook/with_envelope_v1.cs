// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class with_envelope_v1 : given.a_webhook_reactor
{
    WebhookAdded _event;
    EventContext _eventContext;

    void Establish()
    {
        _event = new(WebhookOwner.Client, EventSequenceId.Log, [new EventType("customer", 3)], "https://example.com/webhook", new Dictionary<string, string>(), true, true)
        {
            DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1
        };
        _eventContext = EventContext.From("store", EventStoreNamespaceName.Default, new EventType("webhook-added", 1), EventSourceType.Default, "webhook", EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.New());
    }

    async Task Because() => await _reactor.Added(_event, _eventContext);

    [Fact] void should_keep_the_delivery_format() => _webhooksManager.Received(1).Add(Arg.Is<WebhookDefinition>(definition => definition.DeliveryFormat == WebhookDeliveryFormat.EnvelopeV1));
}
