// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Measures what fencing sink writes costs: fenced against unfenced batches on a single-node and a three-node replica set, for
/// several batch sizes, numbers of open change streams and parallel writers, plus unknown-commit resolution. Writes one JSON
/// line per measurement.
/// </summary>
internal static class FenceCostScenario
{
    /// <summary>
    /// Runs every configuration.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal static async Task Run()
    {
        var image = Environment.GetEnvironmentVariable("FENCE_COST_IMAGE") ?? "mongo:8.2";
        var topologies = Numbers("FENCE_COST_MEMBERS", "1,3");
        var sizes = Numbers("FENCE_COST_SIZES", "100,500,1000");
        var streams = Numbers("FENCE_COST_STREAMS", "0,50,500");
        var parallel = Numbers("FENCE_COST_WRITERS", "8");
        var documents = Numbers("FENCE_COST_DOCUMENTS", "40000")[0];
        foreach (var members in topologies)
        {
            var replicaSet = new FenceCostReplicaSet(members, image);
            try
            {
                await replicaSet.Start();
                await Measure(replicaSet, image, sizes, streams, parallel, documents);
            }
            finally
            {
                await replicaSet.Stop();
            }
        }
    }

    static async Task Measure(FenceCostReplicaSet replicaSet, string image, int[] sizes, int[] streams, int[] parallel, int documents)
    {
        var members = replicaSet.Members;
        Emit(await Describe(replicaSet, image));
        foreach (var streamCount in streams)
        {
            foreach (var size in sizes)
            {
                var target = documents / (1 + (streamCount / 10));
                var batches = Math.Max(streamCount == 0 ? 30 : 5, target / size);
                foreach (var fenced in new[] { false, true })
                {
                    Emit(await FenceCostRun.Measure(new(members, fenced, size, batches, streamCount, 1), replicaSet));
                }
            }
        }

        foreach (var writers in parallel.Where(writers => writers > 1))
        {
            foreach (var fenced in new[] { false, true })
            {
                Emit(await FenceCostRun.Measure(new(members, fenced, 500, Math.Max(10, documents / 500 / writers * 2), 0, writers), replicaSet));
            }
        }

        if (Environment.GetEnvironmentVariable("FENCE_COST_UNKNOWN") != "0")
        {
            Emit(await FenceCostUnknownCommit.Measure(replicaSet, 20, 5));
        }
    }

    static async Task<object> Describe(FenceCostReplicaSet replicaSet, string image)
    {
        using var client = new MongoClient(replicaSet.PrimaryConnectionString);
        var build = await client.GetDatabase("admin").RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1));
        return new { Scenario = "replica-set", replicaSet.Members, Image = image, MongoVersion = build["version"].AsString };
    }

    static void Emit(object measurement)
    {
        var line = JsonSerializer.Serialize(measurement);
        Console.WriteLine(line);
        var output = Environment.GetEnvironmentVariable("FENCE_COST_OUT");
        if (!string.IsNullOrEmpty(output))
        {
            File.AppendAllLines(output, [line]);
        }
    }

    static int[] Numbers(string name, string fallback) =>
        (Environment.GetEnvironmentVariable(name) ?? fallback).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
}
