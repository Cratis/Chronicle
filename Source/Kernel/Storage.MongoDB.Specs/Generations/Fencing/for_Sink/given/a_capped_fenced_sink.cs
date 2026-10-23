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
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing.for_Sink.given;

public class a_capped_fenced_sink(ReplicaSetMongoDBFixture fixture) : Fencing.given.a_fenced_writer_against_a_replica_set(fixture)
{
    protected Sink _sink;
    protected FencedSinkWrites _writes;
    protected IMongoCollection<BsonDocument> _models;

    protected async Task CreateSink(string schemaJson, int cap = 2)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        var definition = new ReadModelDefinition("model", "models", "Model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        await _database.CreateCollectionAsync("models");
        _models = _database.GetCollection<BsonDocument>("models");
        _writes = new(new FixedFenceTokenSource(_token), _executor, cap);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, definition, NullLogger<MongoDBConverter>.Instance);
        var collections = new SinkCollections(definition, _database, null, _writes);
        var changes = new ChangesetConverter(definition, converter, collections, expando, NullLogger<ChangesetConverter>.Instance, _writes);
        _sink = new(definition, converter, collections, changes, expando, new ReadModelChangeStreams(NullLogger<ReadModelChangeStreams>.Instance), NullLogger<Sink>.Instance, _writes);
    }

    protected static Key CompositeKey(int value)
    {
        var id = new ExpandoObject();
        ((IDictionary<string, object?>)id)["a"] = value;
        ((IDictionary<string, object?>)id)["b"] = "part";
        return new(id, ArrayIndexers.NoIndexers);
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> Changes(params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns(changes);
        changeset.HasBeenRemoved().Returns(changes.Any(change => change is Removed));
        return changeset;
    }

    protected static IChangeset<AppendedEvent, ExpandoObject> SettingCount(int count) =>
        Changes(new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("count", null, count)]));

    protected Task<long> Receipts() => _database.GetCollection<BsonDocument>(FenceCollections.Receipts).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

    void Destroy()
    {
        _sink?.Dispose();
        _writes?.Dispose();
    }
}
