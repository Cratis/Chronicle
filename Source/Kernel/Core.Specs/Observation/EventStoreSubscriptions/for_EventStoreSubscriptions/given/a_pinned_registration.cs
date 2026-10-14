// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class a_pinned_registration : an_event_store_subscriptions_service
{
    protected IEventSequence _sequence;
    protected IEventStoreSubscriptionsManager _manager;
    protected AddEventStoreSubscriptions _request;
    protected Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition? _existing;
    protected EventType _pin = new("customer", 3);
    protected Exception _error;

    void Establish()
    {
        _sequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_sequence);
        _sequence.Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));
        _manager = Substitute.For<IEventStoreSubscriptionsManager>();
        _manager.GetSubscriptionDefinitions().Returns(_ => _existing is null ? [] : [_existing]);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(Arg.Any<string>()).Returns(_manager);
        _request = new()
        {
            TargetEventStore = "target",
            Subscriptions = [new() { Identifier = "subscription", SourceEventStore = "source", EventTypes = [new() { Id = _pin.Id, Generation = _pin.Generation }], GenerationDelivery = Contracts.Observation.EventGenerationDelivery.Pinned }]
        };
        Register("source", 1, 2, 3);
        Register("target", 1, 2, 3);
    }

    protected void Register(EventStoreName store, params uint[] generations)
    {
        var schema = JsonSchema.FromJson("{\"type\":\"object\"}");
        var types = _storage.GetEventStore(store).EventTypes;
        types.GetDefinition(_pin.Id).Returns(new EventTypeDefinition(_pin.Id, EventTypeOwner.Client, false, generations.Select(generation => new EventTypeGenerationDefinition(generation, schema)), []));
        foreach (var generation in generations)
        {
            types.HasFor(_pin.Id, generation).Returns(true);
            types.GetFor(_pin.Id, generation).Returns(new EventTypeSchema(new(_pin.Id, generation), EventTypeOwner.Client, EventTypeSource.Code, schema));
        }
    }

    protected int RegistrationEvents() => _sequence.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<EventStoreSubscriptionAdded>().Count();
    protected EventStoreSubscriptionAdded AddedEvent() => _sequence.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<EventStoreSubscriptionAdded>().Single();
    protected Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition Existing(EventGenerationDelivery mode, uint generation) => new("subscription", "source", [new EventType(_pin.Id, generation)]) { GenerationDelivery = mode };
}
