// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_type_schema_is_loaded_during_handling : given.an_observer_with_subscription_for_specific_event_type
{
    EventTypeSchema _schema;

    void Establish()
    {
        _schema = new EventTypeSchema(event_type, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema());
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([_schema]);

        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(42UL));
    }

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_fetch_the_missing_schema_during_handling() => _eventTypesStorage.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());

    [Fact] void should_decrypt_using_the_loaded_schema() => _eventCompliance.Received(1).Release(
        Arg.Any<IEnumerable<AppendedEvent>>(),
        Arg.Is<IDictionary<EventType, EventTypeSchema>>(_ => _.ContainsKey(event_type)));
}
