// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_a_path_is_nested : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("address.street", "Main"))]));

    [Fact] void should_set_the_field_below_the_object() => Json(_stage["address"]).ShouldContain("$setField");
    [Fact] void should_treat_a_missing_or_null_parent_as_an_empty_object() => Json(_stage["address"]).ShouldContain("$type");
    [Fact] void should_keep_the_whole_field_otherwise() => _stage["address"]["$cond"][2].ShouldEqual("$address");
}
