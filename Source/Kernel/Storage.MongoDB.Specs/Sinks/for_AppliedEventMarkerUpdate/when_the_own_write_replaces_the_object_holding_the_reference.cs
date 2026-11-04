// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_AppliedEventMarkerUpdate;

public class when_the_own_write_replaces_the_object_holding_the_reference : given.a_marked_update
{
    BsonDocument _stage;
    BsonDocument _stageAfterUnset;
    BsonDocument _stageAfterReplacingWithoutTheProperty;

    void Because()
    {
        _stage = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("customer", new BsonDocument("id", "B"))), Join(Updates.Set("customerName", "Bea"), "B", onProperty: "customer.id")]));
        _stageAfterUnset = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Unset("customer")), Join(Updates.Set("customerName", "Bea"), "B", onProperty: "customer.id")]));
        _stageAfterReplacingWithoutTheProperty = Stage(AppliedEventMarkerUpdate.Build([Own(Updates.Set("customer", new BsonDocument("name", "x"))), Join(Updates.Set("customerName", "Bea"), "B", onProperty: "customer.id")]));
    }

    [Fact] void should_take_the_reference_from_the_object_the_own_write_sets() => Json(_stage["customerName"]).ShouldContain("\"$literal\" : \"B\" }, \"$customer.id\"");
    [Fact] void should_see_a_removed_object_as_no_reference() => Json(_stageAfterUnset["customerName"]).ShouldContain("\"$literal\" : null }, \"$customer.id\"");
    [Fact] void should_see_a_replacing_object_without_the_property_as_no_reference() => Json(_stageAfterReplacingWithoutTheProperty["customerName"]).ShouldContain("\"$literal\" : null }, \"$customer.id\"");
}
