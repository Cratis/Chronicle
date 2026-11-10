// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Imports;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Imports.when_resuming_the_commit.after_its_job_was_stopped.context;

namespace Cratis.Chronicle.Integration.for_Imports.when_resuming_the_commit;

[Collection(ChronicleCollection.Name)]
public class after_its_job_was_stopped(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        /// <summary>
        /// Enough chunks to stop the commit part way on every storage, while a store that catches up a partition at a time,
        /// as SQL Server does, still delivers every event within the wait.
        /// </summary>
        public const int NumberOfEvents = 6000;

        public override IEnumerable<Type> EventTypes => [typeof(CustomerImported)];
        public override IEnumerable<Type> Reactors => [typeof(ImportedCustomersReactor)];

        public bool Stopped;
        public ImportState StateWhileStopped;
        public ImportResult Result;
        public IImmutableList<AppendedEvent> All;

        async Task Because()
        {
            ImportedCustomersReactor.Handled.Clear();
            await EventStore.Reactors.WaitTillActive<ImportedCustomersReactor>();
            await using var import = await EventStore.EventLog.BeginImport(new ImportOptions { MaxEventsPerChunk = 500, WaitForCommit = false });
            await import.Add(Customers.Stream("restarted", NumberOfEvents));
            await import.Complete();
            await import.WaitTill(_ => _ == ImportState.Committing, TimeSpan.FromSeconds(60));

            // Stopping the commit job is what a kernel shutdown does to it; resuming it is what the restarted kernel does.
            var job = (await EventStore.Jobs.GetJobs()).Single(_ => _.Details.Value == $"Import {import.ImportId.Value}");
            Stopped = (await EventStore.Jobs.Stop(job.Id)).Accepted;
            await EventStore.Jobs.WaitTillJobMeetsPredicate(job.Id, _ => _.Status == JobStatus.Stopped, TimeSpan.FromSeconds(60));
            StateWhileStopped = await import.WaitTill(_ => true, TimeSpan.FromSeconds(10));

            await EventStore.Jobs.Resume(job.Id);
            await import.WaitTill(ImportProgressWaits.IsTerminal, TimeSpan.FromSeconds(180));
            Result = await import.Complete();
            All = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            await EventStore.Reactors.WaitTillReachesEventSequenceNumber<ImportedCustomersReactor>(All[^1].Context.SequenceNumber, TimeSpan.FromMinutes(5));
        }
    }

    [Fact] void should_stop_the_commit_part_way() => Context.Stopped.ShouldBeTrue();
    [Fact] void should_leave_the_import_committing_while_stopped() => Context.StateWhileStopped.ShouldEqual(ImportState.Committing);
    [Fact] void should_complete_after_resuming() => Context.Result.State.ShouldEqual(ImportState.Completed);
    [Fact] void should_commit_every_event_once() => Context.Result.EventsCommitted.ShouldEqual(context.NumberOfEvents);
    [Fact] void should_store_every_event_once() => Context.All.Select(_ => ((CustomerImported)_.Content).Name).Distinct().Count().ShouldEqual(context.NumberOfEvents);
    [Fact] void should_deliver_every_event_to_the_observer_exactly_once() => ImportedCustomersReactor.Handled.Select(_ => _.SequenceNumber).Distinct().Count().ShouldEqual(context.NumberOfEvents);
}
