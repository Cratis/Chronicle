// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_AppendedEventConverters;

public class when_converting_collection_to_client : Specification
{
    class FirstEvent
    {
        public string Value { get; set; } = string.Empty;
    }

    class SecondEvent
    {
        public int Number { get; set; }
    }

    IEnumerable<Contracts.Events.AppendedEvent> _contractEvents;
    IEnumerable<AppendedEvent> _clientEvents;
    IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();

        var firstEventTypeId = new EventTypeId("first-event");
        var secondEventTypeId = new EventTypeId("second-event");

        var firstEventType = new EventType(firstEventTypeId, 1);
        var secondEventType = new EventType(secondEventTypeId, 1);

        _eventTypes.GetClrTypeFor(firstEventTypeId).Returns(typeof(FirstEvent));
        _eventTypes.GetClrTypeFor(secondEventTypeId).Returns(typeof(SecondEvent));

        var firstContext = EventContext.EmptyWithEventSourceId(Guid.NewGuid()) with
        {
            SequenceNumber = 1,
            EventType = firstEventType
        };

        var secondContext = EventContext.EmptyWithEventSourceId(Guid.NewGuid()) with
        {
            SequenceNumber = 2,
            EventType = secondEventType
        };

        var firstEvent = new AppendedEvent(firstContext, new FirstEvent { Value = "test" });
        var secondEvent = new AppendedEvent(secondContext, new SecondEvent { Number = 42 });

        _contractEvents =
        [
            firstEvent.ToContract(JsonSerializerOptions.Default),
            secondEvent.ToContract(JsonSerializerOptions.Default)
        ];
    }

    void Because() => _clientEvents = _contractEvents.ToClient(_eventTypes, JsonSerializerOptions.Default);

    [Fact] void should_return_two_events() => _clientEvents.Count().ShouldEqual(2);
    [Fact] void should_have_first_event_of_correct_type() => _clientEvents.First().Content.ShouldBeOfExactType<FirstEvent>();
    [Fact] void should_have_second_event_of_correct_type() => _clientEvents.Skip(1).First().Content.ShouldBeOfExactType<SecondEvent>();
    [Fact] void should_preserve_first_event_value() => (_clientEvents.First().Content as FirstEvent).Value.ShouldEqual("test");
    [Fact] void should_preserve_second_event_number() => (_clientEvents.Skip(1).First().Content as SecondEvent).Number.ShouldEqual(42);
}
