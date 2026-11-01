// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Benchmarks.Kernel;

/// <summary>
/// Measures what the MongoDB sink's bulk-window child-key lookup costs while a replay streams root and child events through it.
/// </summary>
/// <remarks>
/// The Mongo collection is a no-op fake. A lookup that misses the bulk cache falls through to it and pays a simulated round trip.
/// <see cref="OracleLookup"/> resolves the same lookups through a dictionary maintained by the benchmark, which bounds what any index can save.
/// </remarks>
[MemoryDiagnoser]
[Config(typeof(BenchmarkConfiguration))]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "BenchmarkDotNet disposes the sink of each iteration in IterationCleanup.")]
public class ReplayChildLookupBenchmark
{
    const string ChildrenProperty = "children";
    const string ChildIdProperty = "childId";
    const int UpdatesPerRootInFlatScenario = 20;
    readonly PropertyPath _lookupPath = new("children.childId");
    NoOpMongoCollection _collection = null!;
    Sink _sink = null!;

    /// <summary>
    /// Gets or sets the scenario under measurement.
    /// </summary>
    [ParamsSource(nameof(Scenarios))]
    public ReplayChildLookupScenario Current { get; set; } = null!;

    /// <summary>
    /// Gets the scenarios.
    /// </summary>
    public IEnumerable<ReplayChildLookupScenario> Scenarios =>
    [
        new("Flat-R1000", 1000, 0, false, 0),
        new("R250xC20", 250, 20, false, 0),
        new("R1000xC20", 1000, 20, false, 0),
        new("R1000xC20-rt250", 1000, 20, false, 250),
        new("R500xC20-guid-rt250", 500, 20, true, 250)
    ];

    /// <summary>
    /// Opens a fresh bulk window on a fresh sink, because a replay is stateful.
    /// </summary>
    [IterationSetup]
    public void CreateSink()
    {
        var idSchema = Current.GuidIds ? """{"type":"string","format":"guid"}""" : """{"type":"string"}""";
        var schema = JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"children":{"type":"array","items":{"type":"object","properties":{"childId":ID_SCHEMA,"count":{"type":"integer"}}}}}}
            """.Replace("ID_SCHEMA", idSchema)).GetAwaiter().GetResult();
        var readModel = new ReadModelDefinition("model", "model", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new Cratis.Chronicle.Storage.MongoDB.ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        _collection = NoOpMongoCollection.Create(TimeSpan.FromMicroseconds(Current.MissRoundTripMicroseconds));
        var collections = new BenchmarkSinkCollections(_collection.Collection);
        var changesetConverter = new ChangesetConverter(readModel, converter, collections, expando);
        _sink = new Sink(readModel, converter, collections, changesetConverter, expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance));
        _sink.BeginBulk().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes the sink of the iteration.
    /// </summary>
    [IterationCleanup]
    public void DisposeSink() => _sink.Dispose();

    /// <summary>
    /// Replays the stream resolving each child lookup through <see cref="Sink.TryFindRootKeyByChildValue"/>.
    /// </summary>
    /// <returns>The number of resolved roots.</returns>
    [Benchmark(Baseline = true)]
    public Task<int> SinkLookup() => Replay(oracle: null);

    /// <summary>
    /// Replays the same stream resolving each child lookup through a dictionary, with a round trip on a miss.
    /// </summary>
    /// <returns>The number of resolved roots.</returns>
    [Benchmark]
    public Task<int> OracleLookup() => Replay(oracle: []);

    static ExpandoObject Root(string id, int count)
    {
        var root = new ExpandoObject();
        var values = (IDictionary<string, object?>)root;
        values["id"] = id;
        values["count"] = count;
        values[ChildrenProperty] = new List<object>();
        return root;
    }

    static ExpandoObject WithChild(ExpandoObject cached, ExpandoObject child)
    {
        var copy = new ExpandoObject();
        var values = (IDictionary<string, object?>)copy;
        foreach (var (name, value) in (IDictionary<string, object?>)cached)
        {
            values[name] = value;
        }

        values[ChildrenProperty] = new List<object>((IEnumerable<object>)values[ChildrenProperty]!) { child };
        return copy;
    }

    static ExpandoObject Child(object id)
    {
        var child = new ExpandoObject();
        var values = (IDictionary<string, object?>)child;
        values[ChildIdProperty] = id;
        values["count"] = 0;
        return child;
    }

    static Changeset<AppendedEvent, ExpandoObject> Changes(ExpandoObject initialState, Change change)
    {
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, initialState);
        changeset.Add(change);
        return changeset;
    }

    async Task<int> Replay(Dictionary<string, Key>? oracle)
    {
        var resolved = 0;
        ulong sequence = 0;
        var roots = Current.Roots;
        var children = Current.Children;
        var keys = new Key[roots];
        var childIds = new object[roots][];

        for (var r = 0; r < roots; r++)
        {
            keys[r] = new Key($"root-{r}", ArrayIndexers.NoIndexers);
            await _sink.ApplyChanges(keys[r], Changes(Root($"root-{r}", 0), new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("count", null, 1)])), new EventSequenceNumber(sequence++));
            childIds[r] = new object[children];

            for (var i = 0; i < children; i++)
            {
                object id = Current.GuidIds ? Guid.NewGuid() : $"child-{r}-{i}";
                childIds[r][i] = id;
                if (await Resolve(id, oracle, keys[r]) is not null)
                {
                    resolved++;
                }

                var cached = (await _sink.FindOrDefault(keys[r]))!;
                var child = Child(id);
                var indexers = new ArrayIndexers([new ArrayIndexer(ChildrenProperty, ChildIdProperty, id)]);
                await _sink.ApplyChanges(keys[r], Changes(WithChild(cached, child), new ChildAdded(child, ChildrenProperty, ChildIdProperty, id, indexers)), new EventSequenceNumber(sequence++));
            }
        }

        for (var i = 0; i < children; i++)
        {
            for (var r = 0; r < roots; r++)
            {
                var id = childIds[r][i];
                if (await Resolve(id, oracle, null) is not null)
                {
                    resolved++;
                }

                var cached = (await _sink.FindOrDefault(keys[r]))!;
                var indexers = new ArrayIndexers([new ArrayIndexer("[children]", ChildIdProperty, id)]);
                await _sink.ApplyChanges(keys[r], Changes(cached, new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("[children].count", 0, 1, indexers)])), new EventSequenceNumber(sequence++));
            }
        }

        for (var u = 0; u < UpdatesPerRootInFlatScenario && children == 0; u++)
        {
            for (var r = 0; r < roots; r++)
            {
                var cached = (await _sink.FindOrDefault(keys[r]))!;
                await _sink.ApplyChanges(keys[r], Changes(cached, new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("count", u, u + 1)])), new EventSequenceNumber(sequence++));
                resolved++;
            }
        }

        await _sink.FlushBulk();
        return resolved;
    }

    async Task<Key?> Resolve(object id, Dictionary<string, Key>? oracle, Key? registerAs)
    {
        if (oracle is null)
        {
            var result = await _sink.TryFindRootKeyByChildValue(_lookupPath, id);
            return result.TryGetValue(out var key) ? key : null;
        }

        var text = id.ToString()!;
        if (oracle.TryGetValue(text, out var found))
        {
            return found;
        }

        _collection.SimulateRoundTrip();
        if (registerAs is { } register)
        {
            oracle[text] = register;
        }

        return null;
    }

    sealed class BenchmarkConfiguration : ManualConfig
    {
        public BenchmarkConfiguration() => AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithLaunchCount(1)
            .WithWarmupCount(1)
            .WithIterationCount(5)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));
    }
}
