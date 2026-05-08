// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;
using ContractEventStoreSubscriptionDefinition = Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class a_new_subscription : given.an_event_store_subscriptions_service
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

        _subscriptionsManager = Substitute.For<IEventStoreSubscriptionsManager>();
        _subscriptionsManager.GetSubscriptionDefinitions().Returns([]);
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
                    EventTypes = []
                }
            ]
        };
    }

    async Task Because() => await _service.Add(_request);

    [Fact] void should_append_subscription_added_event() =>
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

    [Fact] void should_wait_until_subscription_is_ready() =>
        _subscriptionsManager.Received(1).WaitUntilSubscribed(
            Arg.Is<EventStoreSubscriptionId>(id => id.Value == "test-subscription-id"),
            Arg.Any<TimeSpan>());

    [Fact] void should_trigger_subscription_reactivation_for_the_source_event_store() =>
        _subscriptionsManager.Received(1).SourceEventStoreAdded(Arg.Is<Concepts.EventStoreName>(name => name.Value == "source-event-store"));
}
