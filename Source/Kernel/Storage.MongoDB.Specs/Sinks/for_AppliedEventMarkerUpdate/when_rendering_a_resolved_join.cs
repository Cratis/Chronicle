// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_rendering_a_resolved_join : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(null), Join(Updates.Set("customerName", "Ada"), "A")]));

    [Fact] void should_require_the_document_to_reference_the_join_key() => Json(_stage["customerName"]).ShouldContain("\"$literal\" : \"A\"");
    [Fact] void should_guard_the_data_and_the_slot_with_the_same_predicate() => _stage["customerName"]["$cond"][0].ShouldEqual(_stage[JoinSlot]["$cond"][0]);
    [Fact] void should_guard_the_own_slot_on_its_own() => Json(_stage[OwnSlot]).ShouldContain("own-stream");
    [Fact] void should_allow_a_fresh_reference_to_apply() => Json(_stage[JoinSlot]).ShouldContain("$ne");
}
