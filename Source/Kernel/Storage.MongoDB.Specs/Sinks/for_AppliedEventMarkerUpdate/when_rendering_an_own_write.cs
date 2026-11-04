// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_rendering_an_own_write : given.a_marked_update
{
    BsonDocument _stage;
    UpdateDefinition<BsonDocument> _update;

    void Because()
    {
        _update = AppliedEventMarkerUpdate.Build([Own(Updates.Set("name", "Alice").Max("__lastHandledEventSequenceNumber", 5L))]);
        _stage = Stage(_update);
    }

    [Fact] void should_guard_the_data_with_the_slot() => Json(_stage["name"]).ShouldContain("$lt");
    [Fact] void should_write_the_value_literally() => Json(_stage["name"]).ShouldContain("\"$literal\" : \"Alice\"");
    [Fact] void should_record_the_slot_under_the_same_guard() => Json(_stage[OwnSlot]).ShouldContain("own-stream");
    [Fact] void should_keep_the_current_value_otherwise() => Json(_stage["name"]).ShouldContain("\"$name\"");
    [Fact] void should_move_the_watermark_forward() => Json(_stage["__lastHandledEventSequenceNumber"]).ShouldContain("$max");
    [Fact] void should_be_registered_as_a_marked_update() => AppliedEventMarkerUpdates.IsMarked(_update).ShouldBeTrue();
}
