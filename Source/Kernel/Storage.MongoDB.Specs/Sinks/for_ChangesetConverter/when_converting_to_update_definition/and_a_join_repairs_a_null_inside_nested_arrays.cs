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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_join_repairs_a_null_inside_nested_arrays : Specification
{
    BsonDocument _repairFilter;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""
            {"type":"object","properties":{"id":{"type":"string"},"joinKey":{"type":"string"},"items":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"info":{"type":"object","properties":{"name":{"type":"string"}}}}}}}}}}}
            """);
        var readModel = new ReadModelDefinition("array-model", "ArrayModel", "array-model", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { { ReadModelGeneration.First, schema } }, []);
        var formats = new TypeFormats();
        var expando = new ExpandoObjectConverter(formats);
        var converter = new MongoDBConverter(expando, formats, readModel, NullLogger<MongoDBConverter>.Instance);
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(collection);
        var filters = new List<FilterDefinition<BsonDocument>>();
        collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null)))
            .AndDoes(call => filters.Add(call.ArgAt<FilterDefinition<BsonDocument>>(0)));
        var indexers = new ArrayIndexers(
        [
            new ArrayIndexer("[items]", "id", "first"),
            new ArrayIndexer("[items].[children]", "id", "child-1")
        ]);
        var joined = new Joined(
            new ExpandoObject(),
            "common",
            "joinKey",
            ArrayIndexers.NoIndexers,
            [
                new PropertiesChanged<ExpandoObject>(new ExpandoObject(),
                [
                    new PropertyDifference("[items].[children].info.name", null, "Again", indexers)
                ])
            ]);
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([joined]);
        await new ChangesetConverter(readModel, converter, collections, expando)
            .ToUpdateDefinition(new Key("common", ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.Unavailable);
        _repairFilter = filters[0].Render(new RenderArgs<BsonDocument>(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));
    }

    [Fact] void should_filter_the_repair_to_the_identified_null_child() => _repairFilter.ShouldEqual(BsonDocument.Parse("""
        { "joinKey": "common", "items": { "$elemMatch": { "_id": "first", "children": { "$elemMatch": { "_id": "child-1", "info": { "$type": "null" } } } } } }
        """));
}
