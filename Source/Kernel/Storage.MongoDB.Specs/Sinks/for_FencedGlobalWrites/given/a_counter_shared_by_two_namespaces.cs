// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites.given;

/// <summary>
/// Two sinks, one per namespace, over the very same collection of a real MongoDB: what a globally scoped read model looks like.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class a_counter_shared_by_two_namespaces(MongoDBFixture fixture) : Specification
{
    protected const string NamespaceA = "namespace-a";
    protected const string NamespaceB = "namespace-b";

    IMongoClient _client = default!;
    string _databaseName = default!;

    protected Sink _sinkA = default!;
    protected Sink _sinkB = default!;
    protected IMongoCollection<BsonDocument> _collection = default!;

    void Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var readModel = CreateReadModelDefinition();
        _sinkA = CreateSink(readModel, database);
        _sinkB = CreateSink(readModel, database);
        _collection = database.GetCollection<BsonDocument>(readModel.ContainerName);
    }

    async Task Destroy()
    {
        if (_databaseName is not null)
        {
            await _client.DropDatabaseAsync(_databaseName);
        }
    }

    protected static Key KeyOf(string id) => new(id, ArrayIndexers.NoIndexers);

    /// <summary>
    /// Reads the instance, adds one to its count and writes it back with a revision check, the way a pipeline attempt does.
    /// </summary>
    /// <param name="sink">The sink of the namespace writing.</param>
    /// <param name="namespaceName">The writing namespace.</param>
    /// <param name="key">The key of the instance.</param>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <returns>Awaitable task.</returns>
    protected static async Task Increment(ISink sink, string namespaceName, Key key, ulong sequenceNumber)
    {
        var initial = await sink.FindOrDefault(key, SinkWriteMode.FencedGlobal) ?? new ExpandoObject();
        var changeset = CountSetTo(initial, namespaceName, sequenceNumber);
        await sink.ApplyChanges(key, changeset, sequenceNumber, SinkWriteMode.FencedGlobal);
    }

    /// <summary>
    /// Increments like <see cref="Increment"/> but re-reads and retries when a write finds the instance changed.
    /// </summary>
    /// <param name="sink">The sink of the namespace writing.</param>
    /// <param name="namespaceName">The writing namespace.</param>
    /// <param name="key">The key of the instance.</param>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <returns>Awaitable task.</returns>
    protected static async Task IncrementWithRetry(ISink sink, string namespaceName, Key key, ulong sequenceNumber)
    {
        while (true)
        {
            try
            {
                await Increment(sink, namespaceName, key, sequenceNumber);
                return;
            }
            catch (SinkWriteConflictException)
            {
                // The conflicting write already completed; re-read and try again straight away.
            }
        }
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> CountSetTo(ExpandoObject initial, string namespaceName, ulong sequenceNumber)
    {
        var current = ((IDictionary<string, object?>)initial).TryGetValue("count", out var existing) && existing is not null
            ? Convert.ToInt32(existing, CultureInfo.InvariantCulture)
            : 0;
        return ChangesetSettingCount(initial, current + 1, namespaceName, sequenceNumber);
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingCount(ExpandoObject initial, int count, string namespaceName, ulong sequenceNumber)
    {
        var state = new ExpandoObject();
        foreach (var (name, value) in (IDictionary<string, object?>)initial)
        {
            ((IDictionary<string, object?>)state)[name] = value;
        }

        ((IDictionary<string, object?>)state)["count"] = count;

        var previous = ((IDictionary<string, object?>)initial).TryGetValue("count", out var existing) ? existing : null;
        PropertyDifference[] differences = [new PropertyDifference(new PropertyPath("count"), previous, count)];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        var incoming = new AppendedEvent(EventContext.Empty with { Namespace = namespaceName, SequenceNumber = sequenceNumber }, new ExpandoObject());
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Incoming.Returns(incoming);
        changeset.InitialState.Returns(initial);
        changeset.CurrentState.Returns(state);
        Change[] changes = [propertiesChanged];
        changeset.Changes.Returns(changes);
        changeset.HasBeenRemoved().Returns(false);
        changeset.HasJoined().Returns(false);
        return changeset;
    }

    protected async Task<BsonDocument> StoredDocument(string id) =>
        await _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).SingleAsync();

    static Sink CreateSink(ReadModelDefinition readModel, IMongoDatabase database)
    {
        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
        var collections = new SinkCollections(readModel, database);
        var mongoDBConverter = new MongoDBConverter(expandoObjectConverter, typeFormats, readModel, NullLogger<MongoDBConverter>.Instance);
        var changesetConverter = new ChangesetConverter(readModel, mongoDBConverter, collections, expandoObjectConverter);
        return new Sink(readModel, mongoDBConverter, collections, changesetConverter, expandoObjectConverter, Substitute.For<IReadModelChangeStreams>());
    }

    static ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-shared-counter-read-model",
            "TestSharedCounterReadModel",
            $"shared_counters_{Guid.NewGuid():N}",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromType<Counter>() }
            },
            []);

    record Counter(string Id, int Count);
}
