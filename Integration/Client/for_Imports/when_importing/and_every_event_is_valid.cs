// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Imports.when_importing.and_every_event_is_valid.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_importing;

[Collection(ChronicleCollection.Name)]
public class and_every_event_is_valid(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEvents = 2500;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerImported)];
        public override IEnumerable<Type> Reactors => [typeof(ImportedCustomersReactor)];

        public ImportResult Result;
        public IImmutableList<AppendedEvent> Appended;

        async Task Because()
        {
            ImportedCustomersReactor.Handled.Clear();
            await EventStore.Reactors.WaitTillActive<ImportedCustomersReactor>();
            Result = await EventStore.EventLog.Import(Customers.Stream("valid", NumberOfEvents), new ImportOptions { MaxEventsPerChunk = 400 });
            Appended = await EventStore.EventLog.GetFromSequenceNumber(Result.FirstSequenceNumber!);
            await EventStore.Reactors.WaitTillReachesEventSequenceNumber<ImportedCustomersReactor>(Result.LastSequenceNumber!);
        }
    }

    Causation ImportCausation(AppendedEvent @event) => @event.Context.Causation.Single(_ => _.Type == "Import");

    [Fact] void should_complete() => Context.Result.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_commit_every_event_once() => Context.Result.EventsCommitted.ShouldEqual(context.NumberOfEvents);
    [Fact] void should_store_every_event() => Context.Appended.Count(_ => _.Context.EventType.Id == typeof(CustomerImported).GetEventType().Id).ShouldEqual(context.NumberOfEvents);
    [Fact] void should_keep_when_each_event_occurred() => Context.Appended[10].Context.Occurred.ShouldEqual(Customers.Epoch.AddMinutes(10));
    [Fact] void should_record_the_import_in_the_causation() => ImportCausation(Context.Appended[0]).Properties["importId"].ShouldEqual(Context.Result.ImportId.Value.ToString());
    [Fact] void should_keep_the_source_sequence_number() => ImportCausation(Context.Appended[5]).Properties["sourceSequenceNumber"].ShouldEqual("1005");
    [Fact] void should_keep_the_source_causation() => Context.Appended[3].Context.Causation.ShouldContain(_ => _.Type == "LegacySystem");
    [Fact] void should_deliver_each_event_to_the_reactor_once() => ImportedCustomersReactor.Handled.Count(_ => _.Name.StartsWith("valid-", StringComparison.Ordinal)).ShouldEqual(context.NumberOfEvents);
}
