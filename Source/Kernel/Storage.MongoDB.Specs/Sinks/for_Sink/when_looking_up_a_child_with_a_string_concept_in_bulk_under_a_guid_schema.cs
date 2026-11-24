// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;
using Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.given;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink;

public class when_looking_up_a_child_with_a_string_concept_in_bulk_under_a_guid_schema : given.a_bulk_child_removal_from_all
{
    const string RootId = "concept-root";
    Key _result;
    bool _found;

    async Task Establish()
    {
        var readModel = new ReadModelDefinition(
            "SomethingId",
            "Something",
            "Something",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromType<GuidKeyedReadModel>() },
            []);
        var collections = Substitute.For<ISinkCollections>();
        collections.GetCollection().Returns(_collection);
        _sink = new Sink(readModel, _converter, collections, _changesetConverter, Substitute.For<IExpandoObjectConverter>(), Substitute.For<IReadModelChangeStreams>());
        await _sink.BeginBulk();
        await Cache(new Key(new TagConcept(RootId), ArrayIndexers.NoIndexers), new { children = new[] { new { childId = "shared" } } }.AsExpandoObject());
        _converter.ClearReceivedCalls();
        _collection.ClearReceivedCalls();
    }

    async Task Because() => _found = (await _sink.TryFindRootKeyByChildValueInRoot("[children].childId", "shared", new Key(RootId, ArrayIndexers.NoIndexers))).TryGetValue(out _result);

    [Fact] void should_find_the_cached_child() => _found.ShouldBeTrue();
    [Fact] void should_preserve_the_cached_concept_identifier() => _result.Value.ShouldEqual(new TagConcept(RootId));
    [Fact] void should_bypass_the_guid_converter() => _converter.DidNotReceive().ToBsonValue(Arg.Any<Key>());
    [Fact]
    void should_not_query_persisted_state() => _collection.DidNotReceive().FindAsync(
        Arg.Any<FilterDefinition<BsonDocument>>(),
        Arg.Any<FindOptions<BsonDocument, BsonDocument>>(),
        Arg.Any<CancellationToken>());
}
