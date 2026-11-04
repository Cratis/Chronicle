// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_clearing_applied_event_markers_for_a_partition;

[Collection(MongoDBCollection.Name)]
public class and_the_partition_has_an_own_marker(ChronicleInProcessFixture fixture) : given.a_partition_with_markers(fixture)
{
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)));
        await _documents.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", "root"), Builders<BsonDocument>.Update.Set("count", 0));
        await Clear(sink, "root");
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)));
        _root = await Read("root");
    }

    [Fact] void should_apply_the_event_again() => _root["count"].ToInt32().ShouldEqual(1);
}
