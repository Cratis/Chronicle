// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter.when_converting_property_to_mongo_property;

public class with_single_level_array_property_with_array_indexers : given.a_mongodb_converter
{
    MongoDBProperty _result;
    Guid _key;
    ArrayIndexers _arrayIndexers;

    void Establish()
    {
        _key = Guid.NewGuid();
        _arrayIndexers = new ArrayIndexers([new ArrayIndexer("[ArrayProperty]", "Identifier", _key)]);
    }

    void Because() => _result = _converter.ToMongoDBProperty(new PropertyPath("[ArrayProperty]"), _arrayIndexers);

    [Fact] void should_have_the_correct_property_name() => _result.Property.ShouldEqual("ArrayProperty.$[arrayProperty]");
    [Fact] void should_have_array_filter_for_property() => _result.ArrayFilters.First().Document["arrayProperty.Identifier"].ShouldEqual(new BsonBinaryData(_key, GuidRepresentation.Standard));
}
