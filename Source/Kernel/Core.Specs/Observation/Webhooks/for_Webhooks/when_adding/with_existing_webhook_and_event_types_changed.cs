// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grains.EventSequences;
using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Observation.Webhooks.for_Webhooks.when_adding;

public class with_existing_webhook_and_event_types_changed : given.a_webhooks_service_grain
{
    IEventSequence _eventSequence;
    IWebhooks _webhooksGrain;
    Contracts.Observation.Webhooks.AddWebhooks _request;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);

        var existingDefinition = new WebhookDefinition(
            "test-webhook",
            WebhookOwner.Client,
            "event-sequence-id",
            [],
            new WebhookTarget(new WebhookTargetUrl("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>()),
            false,
            true);

        _webhooksGrain = Substitute.For<IWebhooks>();
        _webhooksGrain.GetWebhookDefinitions().Returns([existingDefinition]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(_webhooksGrain);

        var changedProperties = new WebhookDefinitionChangedProperties(
            EventTypesChanged: true,
            TargetUrlChanged: false,
            TargetHeadersChanged: false,
            AuthorizationChanged: false,
            IsReplayableChanged: false,
            IsActiveChanged: false,
            OwnerChanged: false,
            EventSequenceIdChanged: false);

        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, changedProperties));

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

    [Fact] void should_append_event_types_set_event() =>
        _eventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "test-webhook"),
            Arg.Any<EventTypesSetForWebhook>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>());
}
