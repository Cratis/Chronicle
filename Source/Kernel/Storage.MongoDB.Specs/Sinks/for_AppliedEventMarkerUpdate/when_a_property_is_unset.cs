// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_a_property_is_unset : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Unset("name"))]));

    [Fact] void should_remove_the_field_under_the_guard() => _stage["name"]["$cond"][1].ShouldEqual("$$REMOVE");
}
