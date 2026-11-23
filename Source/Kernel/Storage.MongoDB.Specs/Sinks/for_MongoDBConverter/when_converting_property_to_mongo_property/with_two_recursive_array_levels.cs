// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_property_to_mongo_property;

public class with_two_recursive_array_levels : given.a_mongodb_converter
{
    MongoDBProperty _result;

    void Because() => _result = _converter.ToMongoDBProperty("[features].[subFeatures].name", new ArrayIndexers(
    [
        new("[features]", "id", "root"),
        new("[features].[subFeatures]", "id", "parent"),
        new("[features].[subFeatures]", "id", "child")
    ]));

    [Fact] void should_address_each_recursive_level() => _result.Property.ShouldEqual("features.$[features].subFeatures.$[subFeatures0].subFeatures.$[subFeatures1].name");
    [Fact]
    void should_filter_each_level_in_order() => _result.ArrayFilters.Select(_ => _.Document).ShouldEqual(new[]
    {
        new BsonDocument("features._id", "root"),
        new BsonDocument("subFeatures0._id", "parent"),
        new BsonDocument("subFeatures1._id", "child")
    });
}
