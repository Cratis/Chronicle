// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_from_sequence_number.and_event_has_no_explicit_subject.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_from_sequence_number;

[Collection(ChronicleCollection.Name)]
public class and_event_has_no_explicit_subject(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = $"stream-{Guid.NewGuid()}";
        public IImmutableList<AppendedEvent> AppendedEvents { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("some value"));
            AppendedEvents = await EventStore.EventLog.GetFromSequenceNumber(0);
        }
    }

    [Fact] void should_get_the_appended_event() => Context.AppendedEvents.Count.ShouldEqual(1);
    [Fact] void should_use_the_event_source_id_as_subject() => Context.AppendedEvents[0].Context.Subject.Value.ShouldEqual(Context.EventSourceId.Value);
}
