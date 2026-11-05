// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_PerProcessActivation;

[Collection(ChronicleCollection.Name)]
public class when_two_processes_start(ChronicleFixture fixture)
{
    [Fact]
    public async Task should_record_and_complete_one_startup_per_process()
    {
        Assert.SkipUnless(fixture.Options.Mode == ChronicleRuntimeMode.OutOfProcess, "Child processes need a kernel reachable over gRPC.");
        var connection = $"chronicle://localhost:{fixture.KernelGrpcHostPort}/?skipTlsValidation=true";
        var store = $"per-process-{Guid.NewGuid():N}";
        await using var first = ChildProcess.Start(connection, store);
        await using var second = ChildProcess.Start(connection, store);

        // Lifecycle identities are per-process, but either worker can handle either startup.
        var ready = await Task.WhenAll(
            first.WaitForLine("READY", TimeSpan.FromSeconds(120)),
            second.WaitForLine("READY", TimeSpan.FromSeconds(120)));
        ParseReady(ready[0]).ShouldNotEqual(ParseReady(ready[1]));

        using var client = new ChronicleClient(new ChronicleClientOptions
        {
            ConnectionString = new(connection),
            EventStore = store,
            AutoDiscoverAndRegister = false
        });
        var eventStore = await client.GetEventStore(store).WaitAsync(TimeSpan.FromSeconds(60));
        var services = ((IChronicleServicesAccessor)eventStore.Connection).Services;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var completed = await services.Occurrences.ObserveOccurrences(new()
        {
            EventStore = store,
            Namespace = EventStoreNamespaceName.Default.Value,
            ApplicationScope = true,
            Trigger = "startup"
        }).Select(result => result.EnsureSuccess().ToArray()).FirstAsync(rows =>
            rows.Count(row => row.State == OccurrenceState.Completed) >= 2).ToTask(deadline.Token);
        completed.Length.ShouldEqual(2);
        completed.Select(row => row.Id).Distinct().Count().ShouldEqual(2);
        foreach (var row in completed)
        {
            row.State.ShouldEqual(OccurrenceState.Completed);
            row.Receipts.Count().ShouldEqual(1);
            (await eventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, row.Id)).Count.ShouldEqual(1);
        }
        await first.Stop();
        await second.Stop();
    }

    static int ParseReady(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        parts.Length.ShouldEqual(2);
        parts[0].ShouldEqual("READY");
        return int.Parse(parts[1], CultureInfo.InvariantCulture);
    }
}
