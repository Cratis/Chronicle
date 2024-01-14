// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_property_to_mongo_property;

public class with_three_levels_property : given.a_mongodb_converter
{
    MongoDBProperty result;

    void Because() => result = converter.ToMongoDBProperty(new PropertyPath("FirstLevel.SecondLevel.ThirdLevel"), ArrayIndexers.NoIndexers);

    [Fact] void should_have_the_correct_property_name() => result.Property.ShouldEqual("firstLevel.secondLevel.thirdLevel");
    [Fact] void should_not_have_any_array_filters() => result.ArrayFilters.ShouldBeEmpty();
}
