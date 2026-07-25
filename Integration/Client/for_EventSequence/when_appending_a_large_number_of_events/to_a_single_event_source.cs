// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending_a_large_number_of_events.to_a_single_event_source.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_a_large_number_of_events;

[Collection(ChronicleCollection.Name)]
public class to_a_single_event_source(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEvents = 2000;
        public const int BatchSize = 250;

        public EventSourceId EventSourceId { get; } = "ledger";
        public IImmutableList<AppendedEvent> AppendedEvents { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(LedgerEntryRecorded)];

        async Task Establish()
        {
            for (var offset = 0; offset < NumberOfEvents; offset += BatchSize)
            {
                var batch = Enumerable.Range(offset, BatchSize).Select(_ => new LedgerEntryRecorded(_)).ToList();
                await EventStore.EventLog.AppendMany(EventSourceId, batch);
            }
        }

        async Task Because() => AppendedEvents = await EventStore.EventLog.GetFromSequenceNumber(0);
    }

    [Fact] void should_get_every_appended_event() => Context.AppendedEvents.Count.ShouldEqual(context.NumberOfEvents);
    [Fact] Task should_have_the_last_event_at_the_tail() => Context.ShouldHaveTailSequenceNumber(context.NumberOfEvents - 1);
    [Fact] Task should_have_the_next_sequence_number_after_the_tail() => Context.ShouldHaveNextSequenceNumber(context.NumberOfEvents);
    [Fact] void should_number_the_events_contiguously() => Context.AppendedEvents.Select(_ => (int)_.Context.SequenceNumber.Value).ShouldEqual(Enumerable.Range(0, context.NumberOfEvents));
    [Fact] void should_store_the_events_in_append_order() => Context.AppendedEvents.Select(_ => ((LedgerEntryRecorded)_.Content).Ordinal).ShouldEqual(Enumerable.Range(0, context.NumberOfEvents));
}
