// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Imports.when_importing.and_ordinary_appends_interleave.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_importing;

[Collection(ChronicleCollection.Name)]
public class and_ordinary_appends_interleave(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public const int NumberOfEvents = 20000;
        public const int OrdinaryAppends = 50;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerImported)];
        public override IEnumerable<Type> Reactors => [typeof(ImportedCustomersReactor)];

        public ImportResult Result;
        public ImportState StateWhenAppending;
        public IImmutableList<AppendedEvent> All;

        async Task Because()
        {
            ImportedCustomersReactor.Handled.Clear();
            await EventStore.Reactors.WaitTillActive<ImportedCustomersReactor>();
            await using var import = await EventStore.EventLog.BeginImport(new ImportOptions { MaxEventsPerChunk = 1000, WaitForCommit = false });
            await import.Add(Customers.Stream("interleaved", NumberOfEvents));
            await import.Complete();

            // The ordinary appends start only once the commit is under way, so they land between imported chunks.
            StateWhenAppending = await import.WaitTill(_ => _ == ImportState.Committing, TimeSpan.FromSeconds(60));
            for (var index = 0; index < OrdinaryAppends; index++)
            {
                await EventStore.EventLog.Append($"ordinary-{index}", new CustomerImported($"ordinary-{index}"));
            }

            await import.WaitTill(ImportProgressWaits.IsTerminal, TimeSpan.FromSeconds(120));
            Result = await import.Complete();
            All = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            await EventStore.Reactors.WaitTillReachesEventSequenceNumber<ImportedCustomersReactor>(All[^1].Context.SequenceNumber);
        }

        public bool IsImported(AppendedEvent @event) => ((CustomerImported)@event.Content).Name.StartsWith("interleaved-", StringComparison.Ordinal);
    }

    [Fact] void should_append_while_the_import_commits() => Context.StateWhenAppending.ShouldEqual(ImportState.Committing);
    [Fact] void should_complete_the_import() => Context.Result.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_keep_every_ordinary_append() => Context.All.Count(_ => ((CustomerImported)_.Content).Name.StartsWith("ordinary-", StringComparison.Ordinal)).ShouldEqual(context.OrdinaryAppends);
    [Fact] void should_keep_every_imported_event() => Context.All.Count(Context.IsImported).ShouldEqual(context.NumberOfEvents);
    [Fact] void should_have_ordinary_appends_between_imported_events() => Context.All.SkipWhile(_ => !Context.IsImported(_)).Reverse().SkipWhile(_ => !Context.IsImported(_)).Any(_ => !Context.IsImported(_)).ShouldBeTrue();
    [Fact] void should_deliver_every_event_to_the_observer_exactly_once() => ImportedCustomersReactor.Handled.Select(_ => _.SequenceNumber).Distinct().Count().ShouldEqual(ImportedCustomersReactor.Handled.Count);
    [Fact] void should_deliver_every_event_to_the_observer() => ImportedCustomersReactor.Handled.Count.ShouldEqual(Context.All.Count);
}
