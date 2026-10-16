// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Storage.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

/// <summary>
/// The same live-kernel scenarios run against two MongoDB silos and one SQL silo.
/// </summary>
public static class OccurrenceScenarios
{
    public static async Task LogicalStartup(OccurrenceFixture fixture)
    {
        var activation = Guid.NewGuid().ToString("N");
        var signals = new OccurrenceSignals();
        await using var first = await OccurrenceApp.Start(fixture.SiloServices, signals, activation);
        await using var second = await OccurrenceApp.Start(fixture.SecondSiloServices, signals, activation);
        var id = Storage.Occurrences.OccurrenceIdentities.ForLifecycle(Storage.Occurrences.OccurrenceKind.Startup, activation).Value;
        var completed = await second.WaitFor("startup", rows => rows.Any(row => row.Id == id && row.State == OccurrenceState.Completed), application: true);
        completed.Count(row => row.Id == id).ShouldEqual(1);
        completed.Single(row => row.Id == id).Receipts.Count().ShouldEqual(1);
        signals.Effects(id).ShouldEqual(1);
        first.ConnectionId.ShouldNotEqual(second.ConnectionId);
        (await second.Store.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, id)).Count.ShouldEqual(1);
    }

    public static async Task ReclaimAfterTotalRestart(OccurrenceFixture fixture)
    {
        var signals = new OccurrenceSignals();
        string id;
        long epoch;
        await using (var ingress = await OccurrenceApp.Start(fixture.SiloServices, signals, "no-lifecycle", handlers: false, lifecycle: false))
        {
            id = (await ingress.Store.Occurrences.Record(new OccurrenceWork("restart"), Guid.NewGuid().ToString("N"))).Id.Value;
            var claim = await ingress.Service.ClaimOccurrences(new()
            {
                EventStore = Constants.EventStore,
                Namespace = EventStoreNamespaceName.Default.Value,
                Trigger = "p3c-work",
                WorkerId = ingress.ConnectionId,
                LeaseSeconds = 1,
                Max = 1
            }).EnsureSuccess();
            var leased = claim.Claims.Single();
            leased.Id.ShouldEqual(id);
            epoch = leased.Epoch;
        }
        await fixture.CrashAndRestart();
        await fixture.Rehydrate();
        await using var recovery = await OccurrenceApp.Start(fixture.SiloServices, signals, "no-lifecycle", lifecycle: false);
        var completed = (await recovery.WaitFor("p3c-work", rows => rows.Any(row => row.Id == id && row.State == OccurrenceState.Completed))).Single(row => row.Id == id);
        completed.Epoch.ShouldEqual(epoch + 1);
        completed.Attempts.ShouldEqual(2);
        completed.Receipts.Count().ShouldEqual(1);
        signals.Effects(id).ShouldEqual(1);
        (await recovery.Store.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, id)).Count.ShouldEqual(1);
        var stale = await recovery.Service.CompleteOccurrence(new()
        {
            EventStore = Constants.EventStore,
            Namespace = EventStoreNamespaceName.Default.Value,
            Trigger = "p3c-work",
            OccurrenceId = id,
            Epoch = epoch
        }).EnsureSuccess();
        stale.Status.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);
    }

    public static async Task LosingAnInstanceMidLease(OccurrenceFixture fixture)
    {
        var signals = new OccurrenceSignals { PauseFirstWork = true };
        await using var first = await OccurrenceApp.Start(fixture.SecondSiloServices, signals, "no-lifecycle", lifecycle: false);
        var id = (await first.Store.Occurrences.Record(new OccurrenceWork("instance-loss"), Guid.NewGuid().ToString("N"))).Id.Value;
        var entered = await signals.Entered.Task.WaitAsync(TimeSpan.FromSeconds(90));
        await using var survivor = await OccurrenceApp.Start(fixture.SiloServices, signals, "no-lifecycle", lifecycle: false);
        if (fixture.HasTwoSilos) await fixture.CrashSecondSilo();
        await first.DisposeAsync();
        var completed = (await survivor.WaitFor("p3c-work", rows => rows.Any(row => row.Id == id && row.State == OccurrenceState.Completed))).Single(row => row.Id == id);
        completed.Epoch.ShouldEqual(entered.Lease.Epoch + 1);
        completed.Receipts.Count().ShouldEqual(1);
        signals.Effects(id).ShouldEqual(1);
        (await survivor.Store.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, id)).Count.ShouldEqual(1);

        // Restore the two-silo fixture before another scenario uses its second application host.
        if (fixture.HasTwoSilos)
        {
            await survivor.DisposeAsync();
            await fixture.CrashAndRestart();
            await fixture.Rehydrate();
        }
    }

    public static async Task FanOutAfterMidwayCrash(OccurrenceFixture fixture)
    {
        var signals = new OccurrenceSignals();
        await using var ingress = await OccurrenceApp.Start(fixture.SiloServices, signals, Guid.NewGuid().ToString("N"), handlers: false);
        for (var index = 0; index < 101; index++) await fixture.Storage.Namespaces.Ensure(new($"fanout-{index}"));
        fixture.FanOut.Arm();
        var activation = Guid.NewGuid().ToString("N");
        var recorded = await ingress.Service.RecordLifecycleOccurrence(new()
        {
            EventStore = Constants.EventStore,
            ActivationId = activation,
            Kind = Contracts.Storage.Occurrences.OccurrenceKind.Startup,
            AllNamespaces = true
        }).EnsureSuccess();
        await fixture.FanOut.Reached.Task.WaitAsync(TimeSpan.FromSeconds(180));
        var before = (await fixture.Storage.LifecycleFanOut.Get(new(recorded.Id)))!;
        before.Progress.ShouldEqual(100);
        before.Namespaces.Count.ShouldBeGreaterThan(before.Progress);
        await fixture.Storage.Namespaces.Ensure(new("fanout-late"));
        await ingress.DisposeAsync();
        try
        {
            await fixture.CrashAndRestart(fixture.FanOut.Release);
        }
        finally
        {
            fixture.FanOut.Release();
        }
        await fixture.Rehydrate();
        var recovered = (await fixture.Storage.LifecycleFanOut.Get(new(recorded.Id)))!;
        recovered.Progress.ShouldEqual(recovered.Namespaces.Count);
        recovered.Namespaces.ShouldContainOnly(before.Namespaces);
        recovered.Namespaces.ShouldNotContain("fanout-late");
        (await fixture.Storage.GetNamespace(new("fanout-late")).Occurrences.GetForTrigger(new("startup"))).ShouldBeEmpty();
        foreach (var name in recovered.Namespaces)
        {
            var occurrences = (await fixture.Storage.GetNamespace(new(name)).Occurrences.GetForTrigger(new("startup"))).ToArray();
            occurrences.Count(row => row.Id == Storage.Occurrences.OccurrenceIdentities.ForLifecycle(Storage.Occurrences.OccurrenceKind.Startup, activation, name)).ShouldEqual(1);
        }
    }
}
