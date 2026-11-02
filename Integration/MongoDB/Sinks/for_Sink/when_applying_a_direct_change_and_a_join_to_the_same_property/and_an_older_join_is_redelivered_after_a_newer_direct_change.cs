// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_a_direct_change_and_a_join_to_the_same_property;

[Collection(MongoDBCollection.Name)]
public class and_an_older_join_is_redelivered_after_a_newer_direct_change(ChronicleInProcessFixture fixture) : given.a_sink_over_real_mongo(fixture)
{
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        // For a projection keyed by its event source id the older join is applied once: its redelivery is suppressed
        // by the applied event marker, so the newer direct value survives (#5524, previously characterized as count=2).
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        var join = EventOf("joined", "root", 10UL);
        await sink.ApplyChanges(RootKey, ChangesetOf(join, Join(false, ("count", 2))), 10UL, SinkWriteMode.OncePerSourceEvent);
        await sink.ApplyChanges(RootKey, ChangesetOf(EventOf("own", "root", 20UL), Direct(("count", 3))), 20UL, SinkWriteMode.OncePerSourceEvent);
        await sink.ApplyChanges(RootKey, ChangesetOf(join, Join(false, ("count", 2))), 10UL, SinkWriteMode.OncePerSourceEvent);
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_keep_the_newer_direct_value_on_the_root() => _root["count"].ToInt32().ShouldEqual(3);
    [Fact] void should_keep_the_joined_value_on_the_other_row() => _other["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_keep_the_watermark_at_the_newer_event() => WatermarkOf(_root).ShouldEqual(20);
}
