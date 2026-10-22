// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures one <see cref="FenceCostConfiguration"/> against a running replica set.
/// </summary>
internal static class FenceCostRun
{
    const int WarmupBatches = 5;

    /// <summary>
    /// Runs the configuration and returns its measurements.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="replicaSet">The replica set.</param>
    /// <returns>An object holding the measurements, ready to serialize.</returns>
    internal static async Task<object> Measure(FenceCostConfiguration configuration, FenceCostReplicaSet replicaSet)
    {
        var settings = MongoClientSettings.FromConnectionString(replicaSet.PrimaryConnectionString);
        settings.MaxConnectionPoolSize = 200;
        using var client = new MongoClient(settings);
        var database = client.GetDatabase("fence-cost");
        var collection = database.GetCollection<BsonDocument>($"models-{Guid.NewGuid():N}");
        await database.CreateCollectionAsync(collection.CollectionNamespace.CollectionName);
        var writers = await Task.WhenAll(Enumerable.Range(0, configuration.Writers).Select(writer => FenceCostWriter.Create(configuration.Fenced, database, collection, writer)));
        await Task.WhenAll(writers.Select(writer => Repeat(writer, configuration.BatchSize, WarmupBatches, null)));

        var streamSettings = MongoClientSettings.FromConnectionString(replicaSet.PrimaryConnectionString);
        streamSettings.MaxConnectionPoolSize = configuration.ChangeStreams + 20;
        using var streamClient = new MongoClient(streamSettings);
        await using var streams = await FenceCostChangeStreams.Open(streamClient.GetDatabase("fence-cost").GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName), configuration.ChangeStreams);
        var members = Enumerable.Range(0, replicaSet.Members).Select(member => new MongoClient(FenceCostReplicaSet.MemberConnectionString(member))).ToArray();
        try
        {
            var attemptsBefore = writers.Sum(writer => writer.Attempts);
            var before = await Task.WhenAll(members.Select(FenceCostServerStats.Read));
            var latencies = new List<double>[configuration.Writers];
            var clock = Stopwatch.StartNew();
            await Task.WhenAll(writers.Select((writer, index) => Repeat(writer, configuration.BatchSize, configuration.Batches, latencies[index] = [])));
            var elapsed = clock.Elapsed.TotalSeconds;
            var after = await Task.WhenAll(members.Select(FenceCostServerStats.Read));
            var deltas = after.Zip(before, (later, earlier) => later.Since(earlier)).ToArray();
            var all = latencies.SelectMany(writer => writer).Order().ToArray();
            var batches = (double)all.Length;
            var primary = deltas[0];
            return new
            {
                configuration.Members,
                configuration.Fenced,
                configuration.BatchSize,
                configuration.ChangeStreams,
                configuration.Writers,
                Batches = all.Length,
                BatchesPerSecond = batches / elapsed,
                DocumentsPerSecond = batches * configuration.BatchSize / elapsed,
                P50Ms = Percentile(all, 0.50),
                P95Ms = Percentile(all, 0.95),
                P99Ms = Percentile(all, 0.99),
                Retries = configuration.Fenced ? writers.Sum(writer => writer.Attempts) - attemptsBefore - all.Length : 0,
                primary.TransactionsStarted,
                primary.TransactionsCommitted,
                primary.TransactionsAborted,
                PrimaryCpuSecondsPer1000Batches = primary.CpuMicroseconds / 1e6 / batches * 1000,
                AllMembersCpuSecondsPer1000Batches = deltas.Sum(delta => delta.CpuMicroseconds) / 1e6 / batches * 1000,
                InsertsPerBatch = primary.Inserts / batches,
                UpdatesPerBatch = primary.Updates / batches,
                CommandsPerBatch = primary.Commands / batches,
                OplogBytesPerBatch = primary.OplogBytes / batches,
                ChangeEventsDelivered = streams.Delivered
            };
        }
        finally
        {
            foreach (var writer in writers)
            {
                writer.Dispose();
            }

            foreach (var member in members)
            {
                member.Dispose();
            }

            await database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
        }
    }

    static async Task Repeat(FenceCostWriter writer, int size, int batches, List<double>? latencies)
    {
        for (var batch = 0; batch < batches; batch++)
        {
            var started = Stopwatch.GetTimestamp();
            await writer.WriteBatch(size);
            latencies?.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }

    static double Percentile(double[] sorted, double percentile) =>
        sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(percentile * sorted.Length) - 1)];
}
