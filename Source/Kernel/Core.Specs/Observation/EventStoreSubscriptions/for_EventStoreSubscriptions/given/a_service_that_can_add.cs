// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.given;

public class a_service_that_can_add : an_event_store_subscriptions_service
{
    protected IEventSequence _eventSequence;
    protected IEventStoreSubscriptionsManager _subscriptionsManager;
    protected IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> _existing = [];

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _eventSequence.Append(
            Arg.Any<Concepts.Events.EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Concepts.Events.Tag>>(),
            Arg.Any<Concepts.Events.EventSourceType>(),
            Arg.Any<Concepts.Events.EventStreamType>(),
            Arg.Any<Concepts.Events.EventStreamId>()).Returns(AppendResult.Success(CorrelationId.New(), Concepts.Events.EventSequenceNumber.First));

        _subscriptionsManager = Substitute.For<IEventStoreSubscriptionsManager>();
        _subscriptionsManager.GetSubscriptionDefinitions().Returns(_ => _existing);
        _grainFactory.GetGrain<IEventStoreSubscriptionsManager>(Arg.Any<string>()).Returns(_subscriptionsManager);
    }
}
