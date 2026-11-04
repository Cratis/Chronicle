// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_an_older_join_is_redelivered(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(10UL), JoinOf(("count", 2)));
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Apply(sink, JoinEvent(10UL), JoinOf(("count", 2)));
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_keep_the_newer_joined_value_on_the_root() => _root["count"].ToInt32().ShouldEqual(3);
    [Fact] void should_keep_the_newer_joined_value_on_the_other_row() => _other["count"].ToInt32().ShouldEqual(3);
}
