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
        if (host is not IQueryPlanHost plans)
        {
            await File.AppendAllTextAsync(path, $"# Query plans ({size} events)\n\n{StorageProviderNames.NameOf(host.Provider)}: not captured.\n");
            return true;
        }

        try
        {
            RunBudget.Current.ThrowIfExpired();
            var storage = host.CreateSequence();
            await StorageBenchmark.Seed(storage, size);
            var prefix = $"{size}/";
            await Capture(plans.Capture, prefix + "cursor-page", async () => await FirstPage(await storage.GetFromSequenceNumber(0)));
            await Capture(plans.Capture, prefix + "cursor-deep", async () => await FirstPage(await storage.GetFromSequenceNumber((ulong)(size / 2))));
            await Capture(plans.Capture, prefix + "type-filtered", async () => await FirstPage(await storage.GetRange(0, (ulong)(size - 1), eventTypes: [BenchmarkEvents.Selected])));
            await Capture(plans.Capture, prefix + "tail", async () => await storage.GetTailSequenceNumber());
            await Capture(plans.Capture, prefix + "tail-per-type", async () => await storage.GetTailSequenceNumbersForEventTypes([BenchmarkEvents.Selected, BenchmarkEvents.Other]));
            await CaptureSequenceReads(plans.Capture, storage, size, prefix);
            var results = new List<(string Label, string Sql, string Plan)>();
            var queries = plans.Capture.Queries.Where(_ => _.Key.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(_ => _.Key, StringComparer.Ordinal).ToArray();
            foreach (var (key, query) in queries)
            {
                RunBudget.Current.ThrowIfExpired();
                var label = key[prefix.Length..];
                var rawPlanPath = Path.Combine(directory, $"plans-sqlserver-{size}-{label}.xml");
                results.Add((label, query.Sql, await plans.Explain(query, rawPlanPath)));
            }

            var first = queries.Select(_ => _.Value).FirstOrDefault();
            var indexes = first is null ? "not captured: no SELECT found" : await plans.IndexDefinitions(first);
            await File.AppendAllTextAsync(path, PlanReport.Format(StorageProviderNames.NameOf(host.Provider), size, results) + "\n## Index definitions\n\n```text\n" + indexes + "\n```\n");
        }
        catch (Exception error)
        {
            await File.AppendAllTextAsync(path, $"# Query plans ({size} events)\n\nnot captured: {error.Message}\n");
        }

        return true;
    }

    static async Task CaptureSequenceReads(QueryCapture capture, IEventSequenceStorage storage, int size, string prefix)
    {
        var middle = (ulong)(size / 2);
        var last = (ulong)(size - 1);
        await Capture(capture, prefix + "source-filtered", async () => await FirstPage(await storage.GetRange(0, last, eventSourceId: BenchmarkEvents.SourceAt(0))));
        await Capture(capture, prefix + "source-type-filtered", async () => await FirstPage(await storage.GetRange(0, last, eventSourceId: BenchmarkEvents.SourceAt(0), eventTypes: [BenchmarkEvents.Selected])));
        await Capture(capture, prefix + "head", async () => await storage.GetHeadSequenceNumber());
        await Capture(capture, prefix + "next", async () => await storage.GetNextSequenceNumberGreaterOrEqualThan(middle));
        await Capture(capture, prefix + "metadata", async () => await storage.GetMetadataAt([0, middle, last]));
        await Capture(capture, prefix + "count", async () => await storage.GetCount());
        await Capture(capture, prefix + "parent-predecessor", async () => await storage.GetInstancesOfAnyInRange([BenchmarkEvents.SourceAt(0)], [BenchmarkEvents.Selected.Id], middle, last));
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
