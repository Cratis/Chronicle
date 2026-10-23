// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>Seeds and reads the independently materialized twin projections.</summary>
/// <param name="fixture">The fixture measurement input.</param>
/// <param name="settings">The settings measurement input.</param>
internal sealed class RebuildBaselineDataset(ClusterBenchmarkFixture fixture, RebuildBaselineSettings settings)
{
    /// <summary>The marker source shared by the two read probes.</summary>
    internal const string Marker = "baseline-marker";
    const string Catalog = "baseline-catalog";
    readonly Random _random = new(settings.Seed);
    int _liveIndex;

    /// <summary>Gets the rebuilt projection handler.</summary>
    internal IProjectionHandler Handler { get; private set; } = null!;

    /// <summary>Gets the never-rebuilt reference handler.</summary>
    internal IProjectionHandler Twin { get; private set; } = null!;

    /// <summary>Gets the served collection name of the rebuilt model.</summary>
    /// <remarks>Neither baseline projection overrides its container name, so the client's read-model naming convention applies.</remarks>
    internal string ContainerName => fixture.NamingPolicy.GetReadModelName(typeof(RebuildBaselineModel));

    /// <summary>Gets the served collection name of the independent twin.</summary>
    internal string TwinContainerName => fixture.NamingPolicy.GetReadModelName(typeof(RebuildBaselineTwin));

    /// <summary>
    /// Gets the settings used to seed and measure this dataset.
    /// </summary>
    internal RebuildBaselineSettings Settings => settings;

    /// <summary>Gets the exact number of subscribed historical events including initialization.</summary>
    internal int SubscribedEvents => settings.Size + 2;
    int Roots => settings.Scenario == "S1" ? 1 : Math.Max(1, settings.Size / 10);

    /// <summary>Registers the twins and appends the acknowledged deterministic corpus.</summary>
    /// <param name="deadline">The deadline measurement input.</param>
    /// <returns>A task that completes when both projections cover the seeded corpus.</returns>
    /// <exception cref="InvalidOperationException">A seed append is refused, or a served collection is missing or empty.</exception>
    internal async Task Seed(CancellationToken deadline)
    {
        Handler = await fixture.EventStore1.Projections.Register<RebuildBaselineModel>().WaitAsync(deadline);
        Twin = await fixture.EventStore1.Projections.Register<RebuildBaselineTwin>().WaitAsync(deadline);
        foreach (var handler in new[] { Handler, Twin })
        {
            await fixture.Readiness.WaitForSubscribedObserver(handler.Id, TimeSpan.FromSeconds(settings.TimeoutSeconds)).WaitAsync(deadline);
        }
        await Progress($"Registered and subscribed {Handler.Id} and {Twin.Id}.");
        var appended = 0;
        foreach (var batch in Events().Chunk(500))
        {
            var result = await fixture.EventStore1.EventLog.AppendMany(batch).WaitAsync(deadline);
            if (!result.IsSuccess) throw new InvalidOperationException("Seed append was refused.");
            appended += batch.Length;
        }
        var fence = await Fence();
        await Progress($"Seed acknowledged {appended} events; waiting for both projections to cover {fence}.");
        await Cover(fence, deadline);
        await VerifyServedCollections(deadline);
        await Progress($"Seed covered {fence}.");
    }

    /// <summary>Captures the acknowledged event-log tail.</summary>
    /// <returns>A task containing the acknowledged event-log tail.</returns>
    internal Task<EventSequenceNumber> Fence() => fixture.EventStore1.EventLog.GetTailSequenceNumber();

    /// <summary>Waits for both projections to cover the frozen fence without failed partitions.</summary>
    /// <param name="fence">The fence measurement input.</param>
    /// <param name="deadline">The deadline measurement input.</param>
    /// <returns>A task that completes when both projections cover the fence.</returns>
    /// <exception cref="TimeoutException">A baseline projection did not cover the fence before the deadline.</exception>
    /// <exception cref="InvalidOperationException">A baseline projection has failed partitions.</exception>
    internal async Task Cover(EventSequenceNumber fence, CancellationToken deadline)
    {
        var reported = DateTimeOffset.UtcNow;
        foreach (var handler in new[] { Handler, Twin })
        {
            ProjectionState? state = null;
            while (true)
            {
                if (deadline.IsCancellationRequested) throw new TimeoutException($"{handler.Id} did not cover {fence}: {(state is null ? "no state read" : Describe(state))}.");
                state = await handler.GetState().WaitAsync(deadline);
                if ((await handler.GetFailedPartitions().WaitAsync(deadline)).Any()) throw new InvalidOperationException($"Baseline projection {handler.Id} has failed partitions.");
                if (state.RunningState == ObserverRunningState.Active && state.LastHandledEventSequenceNumber.IsActualValue && state.LastHandledEventSequenceNumber.Value >= fence.Value) break;
                if (DateTimeOffset.UtcNow - reported >= TimeSpan.FromSeconds(10))
                {
                    reported = DateTimeOffset.UtcNow;
                    await Progress($"Waiting for {handler.Id} to cover {fence}: {Describe(state)}.");
                }
                await Task.Delay(50, deadline).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    /// <summary>Lists the read model collections that currently exist, for diagnosing a missing served collection.</summary>
    /// <returns>A task containing the collection names.</returns>
    internal async Task<IReadOnlyList<string>> CollectionNames()
    {
        using var cursor = await fixture.ReadModelDatabase.ListCollectionNamesAsync();
        return await cursor.ToListAsync();
    }

    /// <summary>Appends one live workload event.</summary>
    /// <returns>A task that completes when the live event append is acknowledged.</returns>
    internal Task AppendLive() => Append(NextLive());

    /// <summary>Appends one live marker revision.</summary>
    /// <param name="revision">The revision measurement input.</param>
    /// <returns>A task that completes when the marker append is acknowledged.</returns>
    internal Task AppendMarker(int revision) => Append(new(Marker, new RebuildBaselineValueChanged(Catalog, revision)));

    /// <summary>Requests a rebuild of only the measured projection.</summary>
    /// <returns>A task representing the rebuild request.</returns>
    internal Task Replay() => fixture.EventStore1.Projections.Replay(Handler.Id);

    /// <summary>Reads the served collection UUID used to detect promotion.</summary>
    /// <returns>A task containing the served collection UUID, or null when no collection exists.</returns>
    internal async Task<BsonValue?> Generation()
    {
        using var cursor = await fixture.ReadModelDatabase.ListCollectionsAsync(new ListCollectionsOptions { Filter = new BsonDocument("name", ContainerName) });
        return (await cursor.ToListAsync()).SingleOrDefault()?.GetValue("info").AsBsonDocument.GetValue("uuid");
    }

    /// <summary>Reads only the marker document from the currently served collection.</summary>
    /// <returns>A task containing the visible marker revision, or -1 when absent.</returns>
    internal async Task<int> ReadListMarker()
    {
        var collection = fixture.ReadModelDatabase.GetCollection<BsonDocument>(ContainerName);
        var marker = await collection.Find(new BsonDocument("_id", Marker)).FirstOrDefaultAsync();
        return marker?.GetValue("revision", marker.GetValue("Revision", -1)).ToInt32() ?? -1;
    }

    /// <summary>Reads the marker through kernel key lookup.</summary>
    /// <returns>A task containing the visible marker revision, or -1 when absent.</returns>
    internal async Task<int> ReadKeyMarker() => (await fixture.EventStore1.ReadModels.GetInstanceById<RebuildBaselineModel>(Marker))?.Revision ?? -1;

    /// <summary>Reads the complete served collection by exact document key.</summary>
    /// <param name="containerName">The served read-model collection name.</param>
    /// <param name="deadline">The deadline measurement input.</param>
    /// <returns>A task containing every served document keyed by its exact document identity.</returns>
    internal async Task<IReadOnlyDictionary<string, string>> Snapshot(string containerName, CancellationToken deadline)
    {
        var collection = fixture.ReadModelDatabase.GetCollection<BsonDocument>(containerName);
        var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(deadline);
        return documents.ToDictionary(document => document["_id"].ToJson(), document => document.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson }), StringComparer.Ordinal);
    }

    static string Describe(ProjectionState state) =>
        $"{state.RunningState}, subscribed {state.IsSubscribed}, last handled {state.LastHandledEventSequenceNumber}, next {state.NextEventSequenceNumber}, tail {state.TailEventSequenceNumber}";

    static Task Progress(string message) => Console.Error.WriteLineAsync($"{DateTimeOffset.UtcNow:O} {message}");

    async Task VerifyServedCollections(CancellationToken deadline)
    {
        var names = await CollectionNames().WaitAsync(deadline);
        foreach (var name in new[] { ContainerName, TwinContainerName })
        {
            if (!names.Contains(name, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"No served collection after seeding: '{name}' is not among [{string.Join(", ", names)}].");
            }

            var collection = fixture.ReadModelDatabase.GetCollection<BsonDocument>(name);
            if (await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, new CountOptions { Limit = 1 }, deadline) == 0)
            {
                throw new InvalidOperationException($"Served collection '{name}' is empty after seeding; collections: [{string.Join(", ", names)}].");
            }
        }
    }

    async Task Append(EventForEventSourceId entry)
    {
        var result = await fixture.EventStore1.EventLog.AppendMany([entry]);
        if (!result.IsSuccess) throw new InvalidOperationException("Live append was refused.");
    }

    bool IsRemovalCycle(int index)
    {
        var cycle = index / Roots;
        return cycle % 3 == 1;
    }

    string Root(int index) => $"baseline-{settings.Seed}-{index % Roots:D8}";

    IEnumerable<EventForEventSourceId> Events()
    {
        yield return new(Marker, new RebuildBaselineValueChanged(Catalog, 0));
        yield return new(Catalog, new RebuildBaselineCatalogNamed("baseline"));
        for (var index = 0; index < settings.Size; index++)
        {
            var root = Root(index);
            if (settings.Scenario == "S3" && index >= Roots)
            {
                var child = $"{root}-child-{index}";
                yield return new(child, new RebuildBaselineChildAdded(root, child, $"value-{_random.Next()}"));
            }
            else if (settings.Scenario == "S4" && IsRemovalCycle(index))
            {
                yield return new(root, new RebuildBaselineRemoved());
            }
            else
            {
                yield return new(root, new RebuildBaselineValueChanged(Catalog, _random.Next()));
            }
        }
    }

    EventForEventSourceId NextLive()
    {
        var index = _liveIndex++;
        var root = Root(index);
        if (settings.Scenario == "S3")
        {
            var child = $"{root}-live-{index}";
            return new(child, new RebuildBaselineChildAdded(root, child, $"live-{index}"));
        }
        return settings.Scenario == "S4" && IsRemovalCycle(index)
            ? new(root, new RebuildBaselineRemoved())
            : new(root, new RebuildBaselineValueChanged(Catalog, index));
    }
}
