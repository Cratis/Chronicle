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
        // Characterizes the pre-existing exposure of unguarded join writes: an older join redelivered after a newer direct
        // change restores the older value while the watermark stays at the newer event. This is not fixed here and must not get worse.
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await sink.ApplyChanges(RootKey, ChangesetOf(Join(false, ("count", 2))), 10UL, SinkWriteMode.Always);
        await sink.ApplyChanges(RootKey, ChangesetOf(Direct(("count", 3))), 20UL, SinkWriteMode.Always);
        await sink.ApplyChanges(RootKey, ChangesetOf(Join(false, ("count", 2))), 10UL, SinkWriteMode.Always);
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_restore_the_older_joined_value_on_the_root() => _root["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_write_the_joined_value_to_the_other_row() => _other["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_keep_the_watermark_at_the_newer_event() => WatermarkOf(_root).ShouldEqual(20);
}
