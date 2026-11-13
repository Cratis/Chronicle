// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_clearing_applied_event_markers_for_a_partition;

[Collection(MongoDBCollection.Name)]
public class and_the_partition_is_replayed(ChronicleInProcessFixture fixture) : given.a_partition_with_markers(fixture)
{
    int _afterTheFirst;
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Clear(sink, JoinedStream);
        await Apply(sink, JoinEvent(10UL), JoinOf(("count", 2)));
        _afterTheFirst = (await Read("root"))["count"].ToInt32();
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        _root = await Read("root");
    }

    [Fact] void should_rewrite_the_older_event_first() => _afterTheFirst.ShouldEqual(2);
    [Fact] void should_end_with_the_newer_event() => _root["count"].ToInt32().ShouldEqual(3);
}
