// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Imports;
using context = Cratis.Chronicle.Integration.for_Imports.when_resuming_staging.after_a_dropped_connection.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_resuming_staging;

[Collection(ChronicleCollection.Name)]
public class after_a_dropped_connection(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEvents = 1000;
        public const int SentBeforeTheDrop = 400;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerImported)];

        public ImportId ImportId;
        public long Acknowledged;
        public ImportResult Result;

        async Task Establish()
        {
            var events = new List<EventForEventSourceId>();
            await foreach (var @event in Customers.Stream("resumed", NumberOfEvents).WithCancellation(CancellationToken.None)) events.Add(@event);

            // The first producer stops after sending part of the stream, without completing the import.
            await using (var first = await EventStore.EventLog.BeginImport(new ImportOptions { MaxEventsPerChunk = 100 }))
            {
                ImportId = first.ImportId;
                await first.Add(events.Take(SentBeforeTheDrop));
            }

            Events = events;
        }

        public List<EventForEventSourceId> Events { get; private set; } = [];

        async Task Because()
        {
            await using var resumed = await EventStore.EventLog.BeginImport(ImportId, new ImportOptions { MaxEventsPerChunk = 100 });
            Acknowledged = resumed.EventsAcknowledged;
            await resumed.Add(Events.Skip((int)Acknowledged));
            Result = await resumed.Complete();
        }
    }

    [Fact] void should_continue_after_the_acknowledged_events() => Context.Acknowledged.ShouldEqual(context.SentBeforeTheDrop);
    [Fact] void should_complete() => Context.Result.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_commit_every_event_once() => Context.Result.EventsCommitted.ShouldEqual(context.NumberOfEvents);
}
