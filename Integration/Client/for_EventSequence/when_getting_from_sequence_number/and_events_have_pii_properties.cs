// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_from_sequence_number.and_events_have_pii_properties.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_from_sequence_number;

[Collection(ChronicleCollection.Name)]
public class and_events_have_pii_properties(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEventWithPii Event { get; private set; } = default!;
        public IImmutableList<AppendedEvent> AppendedEvents { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEventWithPii)];

        void Establish() => Event = new SomeEventWithPii("John Doe", "123-45-6789");

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            AppendedEvents = await EventStore.EventLog.GetFromSequenceNumber(0);
        }
    }

    [Fact] void should_get_the_appended_event() => Context.AppendedEvents.Count.ShouldEqual(1);
    [Fact] void should_return_decrypted_pii_property() => ((SomeEventWithPii)Context.AppendedEvents[0].Content).SocialSecurityNumber.ShouldEqual(Context.Event.SocialSecurityNumber);
}
