// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Chaos;

internal static class ChaosRun
{
    static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    public static async Task<int> Run(RunOptions options, CancellationToken cancellation)
    {
        Directory.CreateDirectory(options.Output);
        var runId = Guid.NewGuid().ToString("N");
        var schedule = FaultSchedule.Create(options);
        Console.WriteLine($"Seed: {options.Seed}; run: {runId}; output: {Path.GetFullPath(options.Output)}");
        foreach (var item in schedule) Console.WriteLine($"PLAN {item.At.TotalSeconds:F1}s {item.Kind} resume={item.Resume}");
        await Save(options.Output, "options.json", options, cancellation);
        await Save(options.Output, "schedule.json", schedule, cancellation);
        await using var environment = new DockerEnvironment(runId, options.KeepContainers);
        try
        {
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            startup.CancelAfter(TimeSpan.FromMinutes(3));
            await environment.Start(startup.Token);
            return await Workload(options, environment, schedule, cancellation);
        }
        finally
        {
            using var logs = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await environment.CaptureLogs(options.Output, logs.Token);
            if (options.KeepContainers) Console.WriteLine($"Containers retained. Cleanup: dotnet run --project Chaos/ChaosRig -- cleanup --run-id {runId}");
        }
    }

    static async Task<int> Workload(RunOptions options, DockerEnvironment environment, IReadOnlyList<ScheduledFault> schedule, CancellationToken cancellation)
    {
        var storeName = $"chaos-{environment.RunId}";
        await using var consumer = new ChildWorker("consumer", environment, storeName, options.Output);
        await using var producer = new ChildWorker("producer", environment, storeName, options.Output);
        await consumer.Start(cancellation);
        await producer.Start(cancellation);
        var executor = new FaultExecutor(environment, producer, consumer);
        await executor.Run(schedule, options.Duration, cancellation);
        await executor.Recover(cancellation);
        // Killing fences even an append whose transport timed out. Its prewritten intention remains unknown/intended.
        await producer.Kill();
        if (!consumer.IsRunning) throw new InvalidOperationException("Consumer exited outside a scheduled fault.");
        var sideLedger = new SideLedger(environment.LedgerConnection, environment.RunId);
        using var host = WorkloadHost.Create("reader", environment.Connection, storeName, sideLedger);
        await host.StartAsync(cancellation);
        try
        {
            var store = await host.Services.GetRequiredService<IChronicleClient>().GetEventStore(storeName).WaitAsync(TimeSpan.FromSeconds(60), cancellation);
            // Local metadata is needed for native model reads; the reader must not register or run consumers.
            await store.DiscoverAll().WaitAsync(TimeSpan.FromSeconds(60), cancellation);
            var reader = new Reader(store, new Ledger(environment.LedgerConnection, environment.RunId), sideLedger, new(environment.MongoConnection));
            var snapshot = await Settle(reader, options.Settle, options.Output, cancellation);
            return await Finish(snapshot, options, cancellation);
        }
        finally
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await host.StopAsync(shutdown.Token);
        }
    }

    static async Task<VerdictInput> Settle(Reader reader, TimeSpan budget, string output, CancellationToken cancellation)
    {
        var clock = Stopwatch.StartNew();
        VerdictInput? latest = null;
        Exception? lastError = null;
        var cleanSamples = 0;
        while (clock.Elapsed < budget)
        {
            using var sample = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            sample.CancelAfter(budget - clock.Elapsed);
            try
            {
                latest = await reader.Collect(sample.Token);
                cleanSamples = Verdict.Compare(latest).IsClean ? cleanSamples + 1 : 0;
                // Require repeated settled observations, not a momentary empty work list.
                if (cleanSamples >= 3) return latest;
            }
            catch (Exception exception) when (!cancellation.IsCancellationRequested)
            {
                lastError = exception;
                cleanSamples = 0;
                await File.AppendAllTextAsync(Path.Combine(output, "settle-errors.log"), $"{exception}\n", cancellation);
            }
            var remaining = budget - clock.Elapsed;
            if (remaining > TimeSpan.Zero) await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1), cancellation);
        }
        cancellation.ThrowIfCancellationRequested();
        if (latest is null) throw new InvalidOperationException("Could not collect a complete snapshot before the settle deadline.", lastError);
        // Even a previously clean sample cannot pass if the final drain never stabilized.
        return latest with { Settled = false };
    }

    static async Task<int> Finish(VerdictInput snapshot, RunOptions options, CancellationToken cancellation)
    {
        await Save(options.Output, "snapshot.json", snapshot, cancellation);
        if (options.SelfTestViolation)
        {
            var acknowledged = snapshot.Ledger.FirstOrDefault(entry => entry.Status == "acked" && snapshot.Events.Any(item => item.Id == entry.Id))
                ?? throw new InvalidOperationException("Self-test requires an acknowledged event present in the snapshot.");
            snapshot = snapshot with { Events = snapshot.Events.Where(item => item.Id != acknowledged.Id).ToArray() };
            await Save(options.Output, "self-test-snapshot.json", snapshot, cancellation);
        }
        var verdict = Verdict.Compare(snapshot);
        await Save(options.Output, "verdict.json", verdict, cancellation);
        foreach (var violation in verdict.Violations) Console.WriteLine($"VIOLATION: {violation}");
        Console.WriteLine(VerdictFormatter.Format(verdict, options.Seed));
        return verdict.IsClean ? 0 : 2;
    }

    static Task Save<T>(string directory, string file, T value, CancellationToken cancellation) => File.WriteAllTextAsync(Path.Combine(directory, file), JsonSerializer.Serialize(value, _json), cancellation);
}
