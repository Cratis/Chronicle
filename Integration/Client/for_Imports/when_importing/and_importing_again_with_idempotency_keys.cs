// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Imports;
using context = Cratis.Chronicle.Integration.for_Imports.when_importing.and_importing_again_with_idempotency_keys.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_importing;

[Collection(ChronicleCollection.Name)]
public class and_importing_again_with_idempotency_keys(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEvents = 500;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerImported)];

        public ImportResult First;
        public ImportResult Second;
        public EventSequenceNumber TailAfterFirst;
        public EventSequenceNumber TailAfterSecond;

        async Task Establish()
        {
            First = await EventStore.EventLog.Import(Customers.Stream("keyed", NumberOfEvents, keyed: true));
            TailAfterFirst = await EventStore.EventLog.GetTailSequenceNumber();
        }

        async Task Because()
        {
            Second = await EventStore.EventLog.Import(Customers.Stream("keyed", NumberOfEvents, keyed: true));
            TailAfterSecond = await EventStore.EventLog.GetTailSequenceNumber();
        }
    }

    [Fact] void should_commit_the_first_import() => Context.First.EventsCommitted.ShouldEqual(context.NumberOfEvents);
    [Fact] void should_complete_the_second_import() => Context.Second.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_report_every_event_already_appended() => Context.Second.AlreadyAppended.ShouldEqual(context.NumberOfEvents);
    [Fact] void should_commit_nothing_new() => Context.Second.EventsCommitted.ShouldEqual(0L);
    [Fact] void should_not_append_duplicates() => Context.TailAfterSecond.ShouldEqual(Context.TailAfterFirst);
}
