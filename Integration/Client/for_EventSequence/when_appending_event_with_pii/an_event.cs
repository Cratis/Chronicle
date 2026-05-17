// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_pii.an_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_event_with_pii;

[Collection(ChronicleCollection.Name)]
public class an_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "some-person";
        public SomeEventWithPII Event { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEventWithPII)];

        void Establish()
        {
            Event = new SomeEventWithPII("John Doe", "123-45-6789");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact]
    Task should_return_decrypted_content_when_reading_event() =>
        Context.ShouldHaveAppendedEvent<SomeEventWithPII>(
            EventSequenceNumber.First.Value,
            Context.EventSourceId.Value,
            readEvent =>
            {
                readEvent.Name.ShouldEqual(Context.Event.Name);
                readEvent.SocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
            });
}
