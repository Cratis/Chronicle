// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_property_to_mongo_property;

public class with_three_levels_property : given.a_mongodb_converter
{
    MongoDBProperty _result;

    void Because() => _result = _converter.ToMongoDBProperty(new PropertyPath("FirstLevel.SecondLevel.ThirdLevel"), ArrayIndexers.NoIndexers);

    [Fact] void should_have_the_correct_property_name() => _result.Property.ShouldEqual("FirstLevel.SecondLevel.ThirdLevel");
    [Fact] void should_not_have_any_array_filters() => _result.ArrayFilters.ShouldBeEmpty();
}
