// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Contracts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_removing;

public class a_subscription : given.an_event_store_subscriptions_service
{
    IEventSequence _eventSequence;
    RemoveEventStoreSubscriptions _request;

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
            Arg.Any<EventStreamId>()).Returns(new AppendResult(Concepts.EventSequences.EventSequenceNumber.First, []));

        _request = new RemoveEventStoreSubscriptions
        {
            TargetEventStore = "target-event-store",
            SubscriptionIds = ["test-subscription-id"]
        };
    }

    async Task Because() => await _service.Remove(_request);

    [Fact] void should_append_subscription_removed_event() =>
        _eventSequence.Received(1).Append(
            Arg.Is<EventSourceId>(id => id.Value == "test-subscription-id"),
            Arg.Any<EventStoreSubscriptionRemoved>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>());
}
