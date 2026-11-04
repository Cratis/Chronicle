// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_the_own_write_changes_the_reference : given.a_marked_update
{
    BsonDocument _stage;

    void Because() => _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("customerId", "B")), Join(Updates.Set("customerName", "Bea"), "B")]));

    [Fact] void should_compare_the_reference_the_own_write_would_leave() => Json(_stage["customerName"]).ShouldContain("\"$literal\" : \"B\"");
    [Fact] void should_consider_the_own_write_guard_when_deciding_the_reference() => Json(_stage["customerName"]).ShouldContain("own-stream");
}
