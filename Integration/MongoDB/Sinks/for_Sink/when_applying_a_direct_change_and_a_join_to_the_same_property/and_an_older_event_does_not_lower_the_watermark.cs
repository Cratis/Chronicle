// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_a_direct_change_and_a_join_to_the_same_property;

[Collection(MongoDBCollection.Name)]
public class and_an_older_event_does_not_lower_the_watermark(ChronicleInProcessFixture fixture) : given.a_sink_over_real_mongo(fixture)
{
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await sink.ApplyChanges(RootKey, ChangesetOf(Direct(("count", 1)), Join(true, ("count", 2))), 20UL, SinkWriteMode.Always);
        await sink.ApplyChanges(RootKey, ChangesetOf(Direct(("count", 5)), Join(true, ("count", 6))), 10UL, SinkWriteMode.Always);
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_keep_the_root_watermark() => WatermarkOf(_root).ShouldEqual(20);
    [Fact] void should_keep_the_other_watermark() => WatermarkOf(_other).ShouldEqual(20);
}
