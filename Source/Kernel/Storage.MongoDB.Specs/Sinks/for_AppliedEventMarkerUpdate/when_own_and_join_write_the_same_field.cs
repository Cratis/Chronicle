// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_own_and_join_write_the_same_field : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("name", "own")), Join(Updates.Set("name", "joined"), "A")]));

    [Fact] void should_apply_the_later_group_last() => Json(_stage["name"]).LastIndexOf("joined").ShouldBeGreaterThan(Json(_stage["name"]).IndexOf("own\""));
    [Fact] void should_write_the_field_once() => _stage.Names.Count(_ => _ == "name").ShouldEqual(1);
}
