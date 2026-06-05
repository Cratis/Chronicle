// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_has_explicit_subject : given.an_observer_with_subscription_and_schema_for_event_type
{
    static readonly Subject _subject = new("explicit-subject-value");

    void Establish() =>
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(42UL));

    async Task Because()
    {
        var eventWithSubject = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL) with
        {
            Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL).Context with
            {
                Subject = _subject
            }
        };
        await _observer.Handle("Something", [eventWithSubject]);
    }

    [Fact] void should_use_subject_value_as_identifier() =>
        _eventCompliance.Received(1).Release(
            Arg.Is<IEnumerable<AppendedEvent>>(events => events.Any(e => e.Context.Subject == _subject)),
            Arg.Any<IDictionary<EventType, EventTypeSchema>>());
}
