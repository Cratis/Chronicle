// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.given;

public class a_pinned_registration : a_webhook_registrar
{
    protected Contracts.Observation.Webhooks.WebhookDefinition _definition;
    protected IEventTypesStorage _eventTypes;
    protected IWebhooks _manager;
    protected List<object> _appended;
    protected Exception? _error;

    void Establish()
    {
        _appended = [];
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _eventTypes.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>()).Returns(true);
        _eventTypes.GetDefinition("customer").Returns(new EventTypeDefinition("customer", EventTypeOwner.Client, false, [new(3, JsonSchema.FromJson("{\"type\":\"object\"}"))], []));
        _storage.GetEventStore(Arg.Any<EventStoreName>()).EventTypes.Returns(_eventTypes);
        _manager = Substitute.For<IWebhooks>();
        _manager.GetWebhookDefinitions().Returns([]);
        _grainFactory.GetGrain<IWebhooks>(Arg.Any<string>()).Returns(_manager);
        var sequence = AnEventSequenceThatAppendsSuccessfully();
        sequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>())
            .Returns(call =>
            {
                _appended.Add(call[1]);
                return AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First);
            });
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(sequence);
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.New, null));
        _definition = new()
        {
            Identifier = "webhook",
            GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned,
            EventTypes = [new() { Id = "customer", Generation = 3 }],
            Target = new() { Url = "https://example.com/webhook" }
        };
    }

    protected void ExistingDefinition(bool eventTypesChanged, bool modeChanged)
    {
        _manager.GetWebhookDefinitions().Returns([_definition.ToChronicle() with { GenerationDelivery = EventGenerationDelivery.Compatibility }]);
        var changed = new WebhookDefinitionChangedProperties(eventTypesChanged, false, false, false, false, false, false, false)
        {
            GenerationDeliveryChanged = modeChanged
        };
        _webhookDefinitionComparer.Compare(Arg.Any<WebhookKey>(), Arg.Any<WebhookDefinition>(), Arg.Any<WebhookDefinition>())
            .Returns(new WebhookDefinitionComparisonResult(WebhookDefinitionCompareResult.Different, changed));
    }
}
