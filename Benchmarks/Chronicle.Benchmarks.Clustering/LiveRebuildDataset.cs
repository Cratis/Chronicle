// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Supplies a bounded-key dataset with repeated updates and a shared join, keeping list probes cheap at large event counts.
/// </summary>
/// <param name="fixture">The running MongoDB kernel.</param>
/// <param name="eventCount">The number of historical value updates.</param>
internal sealed class LiveRebuildDataset(ClusterBenchmarkFixture fixture, int eventCount)
{
    /// <summary>
    /// The event source used for live visibility measurements.
    /// </summary>
    internal const string MarkerKey = "live-marker";
    const int KeyCount = 40;
    const string CatalogKey = "catalog";
    const string Category = "joined";

    /// <summary>
    /// Gets the handler for the projection being rebuilt.
    /// </summary>
    internal IProjectionHandler Handler => fixture.EventStore1.Projections.GetHandlerFor<LiveRebuildProjection>();

    /// <summary>
    /// Seeds the historical events and waits for the joined read models to become visible.
    /// </summary>
    /// <returns>A task representing the seeding operation.</returns>
    internal async Task Seed()
    {
        var log = fixture.EventStore1.EventLog;
        await log.Append(MarkerKey, new RebuildValueChanged(CatalogKey, 0));
        for (var index = 0; index < eventCount; index++)
        {
            await log.Append($"source-{index % KeyCount}", new RebuildValueChanged(CatalogKey, index + 1));
        }
        await log.Append(CatalogKey, new RebuildCatalogNamed(Category));
        await Handler.WaitTillActive();
        await Handler.WaitTillReachesEventSequenceNumber(await log.GetTailSequenceNumber());
        await Verify();
    }

    /// <summary>
    /// Appends a live marker revision.
    /// </summary>
    /// <param name="revision">The marker revision to append.</param>
    /// <returns>A task representing the append operation.</returns>
    internal Task AppendMarker(int revision) => fixture.EventStore1.EventLog.Append(MarkerKey, new RebuildValueChanged(CatalogKey, revision));

    /// <summary>
    /// Reads the marker through the served materialized list.
    /// </summary>
    /// <returns>The visible marker revision, or -1 when absent.</returns>
    internal async Task<int> ReadListMarker() =>
        (await fixture.EventStore1.ReadModels.Materialized.GetInstances<LiveRebuildReadModel>(take: KeyCount + 1))
            .SingleOrDefault(model => model.Id == MarkerKey)?.Revision ?? -1;

    /// <summary>
    /// Reads the marker through the kernel key lookup.
    /// </summary>
    /// <returns>The visible marker revision, or -1 when absent.</returns>
    internal async Task<int> ReadKeyMarker() =>
        (await fixture.EventStore1.ReadModels.GetInstanceById<LiveRebuildReadModel>(MarkerKey))?.Revision ?? -1;

    /// <summary>
    /// Requests a replay of the benchmark projection.
    /// </summary>
    /// <returns>A task representing the replay request.</returns>
    internal Task Replay() => fixture.EventStore1.Projections.Replay<LiveRebuildProjection>();

    /// <summary>
    /// Reads the UUID of the currently served MongoDB collection.
    /// </summary>
    /// <returns>The collection UUID, or null when the collection is absent.</returns>
    internal async Task<BsonValue?> ServedGeneration()
    {
        using var cursor = await fixture.ReadModelDatabase.ListCollectionsAsync(new ListCollectionsOptions
        {
            Filter = new BsonDocument("name", Handler.ContainerName.Value)
        });
        var collection = (await cursor.ToListAsync()).SingleOrDefault();
        return collection?.GetValue("info").AsBsonDocument.GetValue("uuid");
    }

    /// <summary>
    /// Verifies the historical keys, their final revisions, and their joined catalog values.
    /// </summary>
    /// <returns>A task representing the verification operation.</returns>
    /// <exception cref="InvalidOperationException">The read models do not match the seeded history.</exception>
    internal async Task Verify()
    {
        var models = (await fixture.EventStore1.ReadModels.Materialized.GetInstances<LiveRebuildReadModel>(take: KeyCount + 1)).ToArray();
        if (models.Length != Math.Min(KeyCount, eventCount) + 1 || models.Any(model => model.Category != Category))
        {
            throw new InvalidOperationException("The seeded/rebuilt list does not contain the expected keys and joined catalog values.");
        }
        for (var key = 0; key < Math.Min(KeyCount, eventCount); key++)
        {
            var completedCycles = (eventCount - key - 1) / KeyCount;
            var expected = key + 1 + (completedCycles * KeyCount);
            if (models.Single(model => model.Id == $"source-{key}").Revision != expected)
            {
                throw new InvalidOperationException($"The seeded/rebuilt key source-{key} has the wrong revision.");
            }
        }
    }
}
