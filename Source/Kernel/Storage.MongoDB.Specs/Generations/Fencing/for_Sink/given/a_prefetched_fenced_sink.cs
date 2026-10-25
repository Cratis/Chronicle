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
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.given;

public class a_prefetched_fenced_sink(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    protected readonly Key _root = new("k", ArrayIndexers.NoIndexers);
    protected Sink _sink;
    protected FencedSinkWrites _writes;
    protected IMongoCollection<BsonDocument> _models;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"joinKey":{"type":"string"}}}
            """);
        var definition = new ReadModelDefinition("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        await _database.CreateCollectionAsync("models");
        _models = _database.GetCollection<BsonDocument>("models");
        _writes = new(new FixedFenceTokenSource(_token), _executor);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, definition, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(definition, _database, NullLogger<SinkCollections>.Instance, _writes, new MongoDBGenerationContainerClaims(_database));
        var changes = new ChangesetConverter(definition, converter, collections, expando, NullLogger<ChangesetConverter>.Instance, _writes);
        _sink = new(definition, converter, collections, changes, expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance), NullLogger<Sink>.Instance, _writes);
        await _models.InsertOneAsync(new BsonDocument { { "_id", "k" }, { "count", 7 }, { "joinKey", "join" } });
        await _sink.BeginBulk();
        await _sink.PrefetchPage([_root]);
    }

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(ExpandoObject state) => new(new ObjectComparer(), null!, state);

    protected static int Count(ExpandoObject state) => Convert.ToInt32(((IDictionary<string, object?>)state)["count"], CultureInfo.InvariantCulture);

    protected Task<BsonDocument> StoredDocument() => _models.Find(new BsonDocument("_id", "k")).SingleAsync();

    void Destroy()
    {
        _sink?.Dispose();
        _writes?.Dispose();
    }
}
