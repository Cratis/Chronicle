// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_clearing_applied_event_markers_for_a_partition;

[Collection(MongoDBCollection.Name)]
public class and_the_markers_are_cleared_twice(ChronicleInProcessFixture fixture) : given.a_partition_with_markers(fixture)
{
    Exception? _error;
    BsonDocument _root;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        await Apply(sink, OwnEvent(10UL), Direct(("count", 1)));
        await Apply(sink, JoinEvent(20UL), JoinOf(("count", 3)));
        await Clear(sink, "root");
        await Clear(sink, JoinedStream);
        _error = await Catch.Exception(async () =>
        {
            await Clear(sink, "root");
            await Clear(sink, JoinedStream);
        });
        _root = await Read("root");
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_leave_no_own_slot() => HasOwnSlot(_root).ShouldBeFalse();
    [Fact] void should_leave_no_join_slot() => HasJoinSlot(_root).ShouldBeFalse();
}
