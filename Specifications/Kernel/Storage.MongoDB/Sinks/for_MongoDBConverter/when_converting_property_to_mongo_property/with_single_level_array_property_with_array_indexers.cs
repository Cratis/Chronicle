// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using MongoDB.Bson;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_property_to_mongo_property;

public class with_single_level_array_property_with_array_indexers : given.a_mongodb_converter
{
    MongoDBProperty result;
    Guid key;
    ArrayIndexers array_indexers;

    void Establish()
    {
        key = Guid.NewGuid();
        array_indexers = new ArrayIndexers(new[] { new ArrayIndexer("[ArrayProperty]", "Identifier", key) });
    }

    void Because() => result = converter.ToMongoDBProperty(new PropertyPath("[ArrayProperty]"), array_indexers);

    [Fact] void should_have_the_correct_property_name() => result.Property.ShouldEqual("arrayProperty.$[arrayProperty]");
    [Fact] void should_have_array_filter_for_property() => result.ArrayFilters.First().Document["arrayProperty.identifier"].ShouldEqual(new BsonBinaryData(key, GuidRepresentation.Standard));
}
