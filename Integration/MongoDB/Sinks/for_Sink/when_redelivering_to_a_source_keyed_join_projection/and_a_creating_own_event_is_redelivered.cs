// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_creating_own_event_is_redelivered(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _first;
    BsonDocument _second;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(10UL, "fresh"), Direct(("count", 1)));
        await Apply(sink, JoinEvent(15UL), JoinOf(("count", 4)));
        _first = await _documents.Find(Builders<BsonDocument>.Filter.Eq("_id", "fresh")).SingleAsync();
        await Apply(sink, OwnEvent(10UL, "fresh"), Direct(("count", 1)));
        _second = await _documents.Find(Builders<BsonDocument>.Filter.Eq("_id", "fresh")).SingleAsync();
    }

    [Fact] void should_create_the_document_on_the_first_delivery() => _first["count"].ToInt32().ShouldEqual(1);
    [Fact] void should_not_fail_or_change_the_document_on_the_redelivery() => _second["count"].ToInt32().ShouldEqual(1);
}
