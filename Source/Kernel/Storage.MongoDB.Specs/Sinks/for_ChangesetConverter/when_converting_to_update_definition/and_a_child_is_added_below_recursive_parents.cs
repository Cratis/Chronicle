// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_child_is_added_below_recursive_parents : given.a_recursive_changeset_converter
{
    Task Because() => Convert(new ChildAdded(Child("slice"), "[features].[subFeatures].[slices]", "id", "slice", new ArrayIndexers([.. _indexers.All, new("[features].[subFeatures].[slices]", "id", "slice")])));

    [Fact] void should_push_below_the_deep_parent() => _update["$push"]["features.$[features].subFeatures.$[subFeatures0].subFeatures.$[subFeatures1].slices"]["_id"].AsString.ShouldEqual("slice");
    [Fact]
    void should_only_filter_the_ancestors() => _result.ArrayFilters.Select(_ => _.Document).ShouldEqual(new[]
    {
        new BsonDocument("features._id", "root"),
        new BsonDocument("subFeatures0._id", "parent"),
        new BsonDocument("subFeatures1._id", "child")
    });
}
