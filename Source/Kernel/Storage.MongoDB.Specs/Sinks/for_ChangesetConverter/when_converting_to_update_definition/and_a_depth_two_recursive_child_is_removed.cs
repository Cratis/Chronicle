// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_depth_two_recursive_child_is_removed : given.a_recursive_changeset_converter
{
    Task Because() => Convert(new ChildRemoved(Child("child"), "[features].[subFeatures]", "id", "child"));

    [Fact] void should_pull_from_the_real_parent_collection() => _update["$pull"].AsBsonDocument.ShouldEqual(new BsonDocument("features.$[features].subFeatures.$[subFeatures].subFeatures", new BsonDocument("_id", "child")));

    [Fact]
    void should_filter_only_the_ancestors() => _result.ArrayFilters.Select(_ => _.Document).ShouldEqual(new[]
    {
        new BsonDocument("features._id", "root"),
        new BsonDocument("subFeatures._id", "parent")
    });
}
