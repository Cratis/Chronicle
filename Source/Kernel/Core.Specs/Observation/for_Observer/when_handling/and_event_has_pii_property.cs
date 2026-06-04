// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_event_has_pii_property : given.an_observer_with_subscription_and_schema_for_event_type
{
    IEnumerable<AppendedEvent> _receivedEvents = [];
    AppendedEvent _decryptedEvent;

    void Establish()
    {
        _decryptedEvent = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL) with
        {
            Content = new ExpandoObject()
        };
        ((IDictionary<string, object?>)_decryptedEvent.Content)["ssn"] = "123-45-6789";

        _eventCompliance
            .DecryptEvents(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(_ => Task.FromResult(new[] { _decryptedEvent }));

        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(callInfo =>
            {
                _receivedEvents = callInfo.ArgAt<IEnumerable<AppendedEvent>>(1);
                return ObserverSubscriberResult.Ok(42UL);
            });
    }

    async Task Because() => await _observer.Handle("Something", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);

    [Fact] void should_call_decrypt_events() =>
        _eventCompliance.Received(1).DecryptEvents(
            Arg.Any<IEnumerable<AppendedEvent>>(),
            Arg.Any<IDictionary<EventType, EventTypeSchema>>());

    [Fact] void should_deliver_decrypted_content_to_subscriber() => _receivedEvents.First().Content.ShouldEqual(_decryptedEvent.Content);
}
