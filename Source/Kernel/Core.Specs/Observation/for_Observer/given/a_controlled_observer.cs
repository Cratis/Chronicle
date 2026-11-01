// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_controlled_observer : an_observer_with_subscription_for_specific_event_type
{
    protected static readonly EventType added_type = new("cb744f74-37be-4831-986f-e5db1fba1672", EventTypeGeneration.First);
    protected static readonly Key partition = "controlled-partition";

    void Establish()
    {
        var members = new Dictionary<EventTypeId, ObserverTypeSetMember>
        {
            [event_type.Id] = new(new(1), null),
            [added_type.Id] = new(new(1), null)
        };
        _stateStorage.State = _stateStorage.State with
        {
            TypeSetControl = new(new(1), new(typeof(IObserverSubscriber).FullName!, ObserverFilters.None), members, [])
        };
        subscription = subscription with
        {
            EventTypes = [event_type, added_type],
            TypeSetRevision = new(1),
            TypeSetMembers = members,
            Targets = [new(SiloAddress.Zero, null) { DeclaredEventTypes = [event_type] }]
        };
        _observer.SetSubscription(subscription);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
    }

    protected static AppendedEvent EventAt(EventType type, ulong sequence) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(type, sequence);
}
