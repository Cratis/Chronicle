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
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page.given;

public abstract class a_page_with_direct_mutations(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    protected readonly Key _root = new("k", ArrayIndexers.NoIndexers);
    JsonSchema _schema;

    async Task Establish() => _schema = await JsonSchema.FromJsonAsync("""
        {"type":"object","properties":{"id":{"type":"string"},"count":{"type":"integer"},"joinKey":{"type":"string"},"children":{"type":"array","items":{"type":"object","properties":{"childId":{"type":"string"},"value":{"type":"string"}}}}}}
        """);

    protected async Task<(Sink Sink, IMongoCollection<BsonDocument> Collection)> BeginPage(string container, bool prepare)
    {
        var definition = new ReadModelDefinition(container, container, container, ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = _schema }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, definition, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(definition, _rawDatabase);
        var sink = new Sink(definition, converter, collections, new ChangesetConverter(definition, converter, collections, expando), expando, Substitute.For<IReadModelChangeStreams>());
        var collection = collections.GetCollection();
        await collection.InsertOneAsync(new BsonDocument
        {
            { "_id", "k" }, { "count", 7 }, { "joinKey", "join" },
            { "children", new BsonArray { new BsonDocument { { "childId", "c" }, { "value", "original" } } } }
        });
        await sink.BeginBulk();
        if (prepare) await sink.PrefetchPage([_root]);
        return (sink, collection);
    }

    protected static Changeset<AppendedEvent, ExpandoObject> Changes(ExpandoObject state) => new(new ObjectComparer(), null!, state);

    protected static ExpandoObject Child()
    {
        dynamic child = new ExpandoObject();
        child.childId = "c";
        child.value = "original";
        return child;
    }
}
