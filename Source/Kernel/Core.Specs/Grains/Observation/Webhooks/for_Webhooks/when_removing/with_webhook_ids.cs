// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.when_removing;

public class with_webhook_ids : given.a_webhooks_service_grain
{
    IEventSequence _eventSequence;
    Contracts.Observation.Webhooks.RemoveWebhooks _request;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);

        _request = new Contracts.Observation.Webhooks.RemoveWebhooks
        {
            EventStore = "test-event-store",
            Webhooks = ["webhook-1", "webhook-2"]
        };
    }

    async Task Because() => await _webhooksService.Remove(_request);

    [Fact] void should_append_webhook_removed_event_for_each_webhook() =>
        _eventSequence.Received(2).Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<WebhookRemoved>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>());
}
