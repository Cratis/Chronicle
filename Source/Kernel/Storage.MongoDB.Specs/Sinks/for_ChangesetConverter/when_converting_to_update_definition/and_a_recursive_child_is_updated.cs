// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ChangesetConverter.when_converting_to_update_definition;

public class and_a_recursive_child_is_updated : given.a_recursive_changeset_converter
{
    Task Because() => Convert(new PropertiesChanged<System.Dynamic.ExpandoObject>(Child("child"), [new PropertyDifference("[features].[subFeatures].name", "child", "Renamed", _indexers)]));

    [Fact] void should_update_the_deep_child() => _update["$set"]["features.$[features].subFeatures.$[subFeatures0].subFeatures.$[subFeatures1].name"].AsString.ShouldEqual("Renamed");
    [Fact]
    void should_filter_each_parent_and_the_child() => _result.ArrayFilters.Select(_ => _.Document).ShouldEqual(new[]
    {
        new BsonDocument("features._id", "root"),
        new BsonDocument("subFeatures0._id", "parent"),
        new BsonDocument("subFeatures1._id", "child")
    });
}
