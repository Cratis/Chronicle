// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_a_value_looks_like_a_field_path : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("name", "$other"))]));

    [Fact] void should_wrap_the_value_as_a_literal() => _stage["name"]["$cond"][1].ShouldEqual(new BsonDocument("$literal", "$other"));
}
