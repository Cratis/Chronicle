// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks.when_adding;

public class with_new_webhook : given.a_webhooks_service_grain
{
    IEventSequence _eventSequence;
    IWebhooks _webhooksGrain;
    Contracts.Observation.Webhooks.AddWebhooks _request;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);

        _webhooksGrain = Substitute.For<IWebhooks>();
        _webhooksGrain.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(_webhooksGrain);

        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));

        _request = new Contracts.Observation.Webhooks.AddWebhooks
        {
            EventStore = "test-event-store",
            Webhooks =
            [
                new ContractWebhookDefinition
                {
                    Identifier = "test-webhook",
                    IsActive = true
                }
            ]
        };
    }

    async Task Because() => await _webhooksService.Add(_request);

    [Fact] void should_append_webhook_added_event() =>
        _eventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "test-webhook"),
            Arg.Any<WebhookAdded>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>());
}
