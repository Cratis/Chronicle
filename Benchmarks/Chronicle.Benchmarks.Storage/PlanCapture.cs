// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Runs labelled, untimed queries on a fresh sequence after measurement.</summary>
internal static class PlanCapture
{
    /// <summary>Writes query plans and index definitions, reporting capture errors without failing measurement.</summary>
    /// <param name="host">The provider host.</param>
    /// <param name="size">The size of the fresh fixture.</param>
    /// <param name="directory">The provider artifact directory.</param>
    /// <returns>Completion, for the harness budget race.</returns>
    public static async Task<bool> Run(IStorageProviderHost host, int size, string directory)
    {
        var path = Path.Combine(directory, "plans.md");
        if (host is not IQueryPlanHost plans || host.Provider == StorageProvider.SqlServer)
        {
            await File.WriteAllTextAsync(path, $"# Query plans\n\n{StorageProviderNames.NameOf(host.Provider)}: not captured.\n");
            return true;
        }

        try
        {
            RunBudget.Current.ThrowIfExpired();
            var storage = host.CreateSequence();
            await StorageBenchmark.Seed(storage, size);
            await Capture(plans.Capture, "cursor-page", async () => await FirstPage(await storage.GetFromSequenceNumber(0)));
            await Capture(plans.Capture, "cursor-deep", async () => await FirstPage(await storage.GetFromSequenceNumber((ulong)(size / 2))));
            await Capture(plans.Capture, "type-filtered", async () => await FirstPage(await storage.GetRange(0, (ulong)(size - 1), eventTypes: [BenchmarkEvents.Selected])));
            await Capture(plans.Capture, "tail", async () => await storage.GetTailSequenceNumber());
            await Capture(plans.Capture, "tail-per-type", async () => await storage.GetTailSequenceNumbersForEventTypes([BenchmarkEvents.Selected, BenchmarkEvents.Other]));
            await CaptureSequenceReads(plans.Capture, storage, size);
            var results = new List<(string Label, string Sql, string Plan)>();
            foreach (var (label, query) in plans.Capture.Queries.OrderBy(_ => _.Key, StringComparer.Ordinal))
            {
                RunBudget.Current.ThrowIfExpired();
                results.Add((label, query.Sql, await plans.Explain(query)));
            }

            var first = plans.Capture.Queries.Values.FirstOrDefault();
            var indexes = first is null ? "not captured: no SELECT found" : await plans.IndexDefinitions(first);
            await File.WriteAllTextAsync(path, PlanReport.Format(StorageProviderNames.NameOf(host.Provider), size, results) + "\n## Index definitions\n\n```text\n" + indexes + "\n```\n");
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(path, $"# Query plans\n\nnot captured: {error.Message}\n");
        }

        return true;
    }

    static async Task CaptureSequenceReads(QueryCapture capture, IEventSequenceStorage storage, int size)
    {
        var middle = (ulong)(size / 2);
        var last = (ulong)(size - 1);
        await Capture(capture, "source-filtered", async () => await FirstPage(await storage.GetRange(0, last, eventSourceId: BenchmarkEvents.SourceAt(0))));
        await Capture(capture, "source-type-filtered", async () => await FirstPage(await storage.GetRange(0, last, eventSourceId: BenchmarkEvents.SourceAt(0), eventTypes: [BenchmarkEvents.Selected])));
        await Capture(capture, "head", async () => await storage.GetHeadSequenceNumber());
        await Capture(capture, "next", async () => await storage.GetNextSequenceNumberGreaterOrEqualThan(middle));
        await Capture(capture, "metadata", async () => await storage.GetMetadataAt([0, middle, last]));
        await Capture(capture, "count", async () => await storage.GetCount());
        await Capture(capture, "parent-predecessor", async () => await storage.GetInstancesOfAnyInRange([BenchmarkEvents.SourceAt(0)], [BenchmarkEvents.Selected.Id], middle, last));
    }

    static async Task Capture(QueryCapture capture, string label, Func<Task> work)
    {
        RunBudget.Current.ThrowIfExpired();
        using (capture.Begin(label)) await work();
    }

    static async Task FirstPage(IEventCursor cursor)
    {
        using (cursor) await cursor.MoveNext();
    }
}
