// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_recursive_child_is_added : given.a_recursive_changeset_converter
{
    Task Because() => Convert(new ChildAdded(Child("nested"), "[features].[subFeatures]", "id", "nested", new ArrayIndexers([.. _indexers.All, new("[features].[subFeatures]", "id", "nested")])));

    [Fact] void should_push_below_the_recursive_parent() => _update["$push"]["features.$[features].subFeatures.$[subFeatures0].subFeatures.$[subFeatures1].subFeatures"]["_id"].AsString.ShouldEqual("nested");
    [Fact]
    void should_keep_the_recursive_ancestor_filters() => _result.ArrayFilters.Select(_ => _.Document).ShouldEqual(new[]
    {
        new BsonDocument("features._id", "root"),
        new BsonDocument("subFeatures0._id", "parent"),
        new BsonDocument("subFeatures1._id", "child")
    });
}
