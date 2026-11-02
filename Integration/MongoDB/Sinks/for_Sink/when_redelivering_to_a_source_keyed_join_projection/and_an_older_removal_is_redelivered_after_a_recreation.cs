// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_an_older_removal_is_redelivered_after_a_recreation(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument? _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(10UL, "gone"), Direct(("count", 1)));
        await Apply(sink, OwnEvent(20UL, "gone"), new Removed(new ExpandoObject()));
        await Apply(sink, OwnEvent(30UL, "gone"), Direct(("count", 2)));
        await Apply(sink, OwnEvent(20UL, "gone"), new Removed(new ExpandoObject()));
        _root = await _documents.Find(Builders<BsonDocument>.Filter.Eq("_id", "gone")).SingleOrDefaultAsync();
    }

    [Fact] void should_keep_the_recreated_document() => _root.ShouldNotBeNull();
    [Fact] void should_keep_the_recreated_value() => _root["count"].ToInt32().ShouldEqual(2);
}
