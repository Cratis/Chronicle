// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using ContractEventStoreSubscriptionDefinition = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class an_existing_subscription_with_different_event_types : given.an_event_store_subscriptions_service
{
    IEventSequence _eventSequence;
    IEventStoreSubscriptionsManager _subscriptionsManager;
    AddEventStoreSubscriptions _request;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), EventSequenceNumber.First));

        var existingDefinition = new Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition(
            new EventStoreSubscriptionId("test-subscription-id"),
            new EventStoreName("source-event-store"),
            [new EventType("existing-event-type", EventTypeGeneration.First)]);

        _subscriptionsManager = Substitute.For<IEventStoreSubscriptionsManager>();
        _subscriptionsManager.GetSubscriptionDefinitions().Returns([existingDefinition]);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(Arg.Any<string>()).Returns(_subscriptionsManager);

        _request = new AddEventStoreSubscriptions
        {
            TargetEventStore = "target-event-store",
            Subscriptions =
            [
                new ContractEventStoreSubscriptionDefinition
                {
                    Identifier = "test-subscription-id",
                    SourceEventStore = "source-event-store",
                    EventTypes =
                    [
                        new Contracts.Events.EventType
                        {
                            Id = "new-event-type",
                            Generation = 1
                        }
                    ]
                }
            ]
        };
    }

    async Task Because() => await _service.Add(_request);

    [Fact] void should_append_subscription_added_event_for_updated_definition() =>
        _eventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "test-subscription-id"),
            Arg.Any<EventStoreSubscriptionAdded>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>());
}
