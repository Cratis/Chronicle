// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_AppendedEventConverters;

public class when_converting_back_and_forth : Specification
{
    class TestEvent
    {
        public string Value { get; set; } = string.Empty;
        public int Number { get; set; }
    }

    AppendedEvent _original;
    Contracts.Events.AppendedEvent _contract;
    AppendedEvent _roundTripped;
    IEventTypes _eventTypes;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        var eventTypeId = new EventTypeId("test-event");
        var eventType = new EventType(eventTypeId, 1);

        var context = EventContext.EmptyWithEventSourceId(Guid.NewGuid()) with
        {
            SequenceNumber = 42,
            EventType = eventType
        };
        var content = new TestEvent { Value = "test", Number = 42 };
        _original = new(context, content);

        _eventTypes.GetClrTypeFor(eventTypeId).Returns(typeof(TestEvent));

        _contract = _original.ToContract(JsonSerializerOptions.Default);
    }

    void Because() => _roundTripped = _contract.ToClient(_eventTypes, JsonSerializerOptions.Default);

    [Fact] void should_preserve_context() => _roundTripped.Context.ShouldEqual(_original.Context);
    [Fact] void should_have_deserialized_content_to_correct_type() => _roundTripped.Content.ShouldBeOfExactType<TestEvent>();
    [Fact] void should_preserve_content_value() => (_roundTripped.Content as TestEvent).Value.ShouldEqual("test");
    [Fact] void should_preserve_content_number() => (_roundTripped.Content as TestEvent).Number.ShouldEqual(42);
}
