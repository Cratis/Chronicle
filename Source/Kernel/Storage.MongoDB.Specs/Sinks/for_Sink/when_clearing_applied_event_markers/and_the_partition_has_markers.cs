// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_clearing_applied_event_markers;

public class and_the_partition_has_markers : Specification
{
    Sink _sink;
    BsonDocument _oneFilter;
    BsonDocument _oneUpdate;
    BsonDocument _manyFilter;
    BsonDocument _manyUpdate;
    string _definitionId;

    void Establish()
    {
        var readModel = new ReadModelDefinition(
            "Orders",
            "orders",
            "Orders",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = new JsonSchema() },
            []);
        var collection = Substitute.For<IMongoCollection<BsonDocument>>();
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(collection);
        var renderArgs = new RenderArgs<BsonDocument>(BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>(), BsonSerializer.SerializerRegistry);
        collection.UpdateOneAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _oneFilter = call.Arg<FilterDefinition<BsonDocument>>().Render(renderArgs);
                _oneUpdate = call.Arg<UpdateDefinition<BsonDocument>>().Render(renderArgs).AsBsonDocument;
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
            });
        collection.UpdateManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<UpdateDefinition<BsonDocument>>(), Arg.Any<UpdateOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _manyFilter = call.Arg<FilterDefinition<BsonDocument>>().Render(renderArgs);
                _manyUpdate = call.Arg<UpdateDefinition<BsonDocument>>().Render(renderArgs).AsBsonDocument;
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(3, 3, null));
            });
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(Arg.Any<Key>()).Returns(new BsonString("partition-1"));
        _sink = new Sink(readModel, converter, collections, Substitute.For<IChangesetConverter>(), Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>());
        _definitionId = AppliedEventMarkers.DefinitionId("event-type", "customerId");
    }

    async Task Because() => await _sink.ClearAppliedEventMarkers(
        "ns",
        "the-sequence",
        new Key("partition-1", ArrayIndexers.NoIndexers),
        [new AppliedEventMarkerJoin(_definitionId, "event-type", "customerId")]);

    [Fact] void should_target_the_document_of_the_partition() => _oneFilter.ShouldEqual(new BsonDocument("_id", "partition-1"));
    [Fact] void should_unset_the_own_slots() => _oneUpdate.ShouldEqual(BsonDocument.Parse("""{ "$unset": { "__applied.f": 1 } }"""));
    [Fact] void should_target_the_documents_joined_by_the_stream_of_the_partition() =>
        _manyFilter.ShouldEqual(new BsonDocument($"__applied.j.{_definitionId}.s", AppliedEventMarkers.StreamHash("ns", "the-sequence", "partition-1")));
    [Fact] void should_unset_the_slot_of_the_join() => _manyUpdate.ShouldEqual(BsonDocument.Parse($$"""{ "$unset": { "__applied.j.{{_definitionId}}": 1 } }"""));
}
