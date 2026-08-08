// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSeeding.when_seeding_a_stream_of_mixed_event_types.context;

namespace Cratis.Chronicle.Integration.for_EventSeeding;

/// <summary>
/// A seeded stream is a history, and a history is an order. The client sends the entries bucketed twice -
/// by event type and by event source - and only the by-event-source bucketing carries the sequence the
/// seeder wrote; bucketing by event type puts every event of the first type before every event of the
/// second. Reconciling from the wrong side hands the event source a history it could never have lived
/// through, which is what a state machine downstream then has to make sense of.
/// </summary>
/// <param name="context">The context the specification runs against.</param>
[Collection(ChronicleCollection.Name)]
public class when_seeding_a_stream_of_mixed_event_types(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public string TheOffice;
        public IEnumerable<string> AppendedEventTypes;

        public override IEnumerable<Type> EventTypes => [typeof(BadgeIssued), typeof(OfficeOpened)];

        void Establish() => TheOffice = Guid.NewGuid().ToString();

        async Task Because()
        {
            EventStore.Seeding.ForEventSource(TheOffice, [
                new OfficeOpened("Oslo"),
                new BadgeIssued("A-1", "First holder"),
                new OfficeOpened("Bergen")
            ]);
            await EventStore.Seeding.Register();

            AppendedEventTypes =
            [
                .. (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First))
                    .OrderBy(_ => _.Context.SequenceNumber.Value)
                    .Select(_ => _.Content.GetType().Name)
            ];
        }
    }

    [Fact] void should_append_every_seeded_event() => Context.AppendedEventTypes.Count().ShouldEqual(3);
    [Fact] void should_append_them_in_the_order_the_seeder_wrote() => Context.AppendedEventTypes.ToArray().ShouldEqual<string[]>([nameof(OfficeOpened), nameof(BadgeIssued), nameof(OfficeOpened)]);
}
