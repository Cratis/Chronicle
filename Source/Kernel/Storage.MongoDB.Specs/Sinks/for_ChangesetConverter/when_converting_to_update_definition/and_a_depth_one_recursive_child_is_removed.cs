// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_depth_one_recursive_child_is_removed : given.a_recursive_changeset_converter
{
    void Establish() => _indexers = new(_indexers.All.Take(2));

    Task Because() => Convert(new ChildRemoved(Child("parent"), "[features].[subFeatures]", "id", "parent"));

    [Fact] void should_keep_the_existing_pull_path_and_identifier() => _update["$pull"].AsBsonDocument.ShouldEqual(new BsonDocument("features.$[features].subFeatures", new BsonDocument("_id", "parent")));
    [Fact] void should_only_filter_the_root_feature() => _result.ArrayFilters.Single().Document.ShouldEqual(new BsonDocument("features._id", "root"));
}
