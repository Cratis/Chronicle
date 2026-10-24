// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_numeric_id_has_a_different_bson_type(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    SinkPagePrefetch _prefetch;
    long _matchingDocuments;
    readonly Key _numeric = new(42L, ArrayIndexers.NoIndexers);
    readonly Key _missing = new("missing", ArrayIndexers.NoIndexers);

    async Task Because()
    {
        var collection = _rawDatabase.GetCollection<BsonDocument>("numeric-ids");
        await collection.InsertOneAsync(new BsonDocument("_id", new BsonInt32(42)));
        var converter = Substitute.For<IMongoDBConverter>();
        converter.ToBsonValue(_numeric).Returns(new BsonInt64(42));
        converter.ToBsonValue(_missing).Returns(new BsonString("missing"));
        _prefetch = new(converter);
        _matchingDocuments = await collection.CountDocumentsAsync(new BsonDocument("_id", new BsonInt64(42)));
        await _prefetch.Prefetch(collection, [_numeric, _missing], (_, _) => { });
    }

    [Fact] void should_exercise_a_real_cross_type_numeric_match() => _matchingDocuments.ShouldEqual(1L);
    [Fact] void should_not_record_the_matched_numeric_id_as_missing() => _prefetch.IsKnownMiss("42").ShouldBeFalse();
    [Fact] void should_not_record_any_misses_for_the_ambiguous_batch() => _prefetch.IsKnownMiss("missing").ShouldBeFalse();
}
