// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.given;

public class a_replayable_bulk_sink(MongoDBFixture fixture) : Specification
{
    protected Sink _sink;
    protected IMongoCollection<BsonDocument> _primary;
    protected IMongoCollection<BsonDocument> _replay;
    protected readonly Key _key = new("counter", ArrayIndexers.NoIndexers);
    protected ReplayContext _context;
    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        _databaseName = MongoDBSpecDatabaseNames.New();
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var readModel = new ReadModelDefinition(
            "bulk-replay", "counters", "BulkReplay", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, JsonSchema.FromType<Model>() } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(readModel, database);
        _sink = new Sink(readModel, converter, collections, new ChangesetConverter(readModel, converter, collections, expando), expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance));
        _primary = database.GetCollection<BsonDocument>("counters");
        _replay = database.GetCollection<BsonDocument>("replay-counters");
        _context = new(new("bulk-replay", ReadModelGeneration.First), "counters", "counters-backup", DateTimeOffset.UnixEpoch);
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(new ObjectComparer(), null!, new ExpandoObject());
        changeset.ReplaceState(state, [new PropertyDifference("count", null, count)]);
        return changeset;
    }

    record Model(string Id, int Count);
}
