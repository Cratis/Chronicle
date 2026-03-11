// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

public class when_converting_bson_document_with_null_values_to_expando_object : given.an_expando_object_converter_with_nullable_properties
{
    BsonDocument _source;
    dynamic _result;

    void Establish()
    {
        _source = new BsonDocument(new BsonElement[]
        {
            new("name", "Test"),
            new("nullableIntValue", BsonNull.Value),
            new("nullableDoubleValue", BsonNull.Value),
            new("nullableBoolValue", BsonNull.Value),
            new("nullableGuidValue", BsonNull.Value),
            new("nullableDecimalValue", BsonNull.Value),
        }.AsEnumerable());
    }

    void Because() => _result = converter.ToExpandoObject(_source, schema);

    [Fact] void should_set_name_correctly() => ((string)_result.name).ShouldEqual("Test");
    [Fact] void should_not_have_nullable_int_value() => ((IDictionary<string, object?>)_result).ContainsKey("nullableIntValue").ShouldBeFalse();
    [Fact] void should_not_have_nullable_double_value() => ((IDictionary<string, object?>)_result).ContainsKey("nullableDoubleValue").ShouldBeFalse();
    [Fact] void should_not_have_nullable_bool_value() => ((IDictionary<string, object?>)_result).ContainsKey("nullableBoolValue").ShouldBeFalse();
    [Fact] void should_not_have_nullable_guid_value() => ((IDictionary<string, object?>)_result).ContainsKey("nullableGuidValue").ShouldBeFalse();
    [Fact] void should_not_have_nullable_decimal_value() => ((IDictionary<string, object?>)_result).ContainsKey("nullableDecimalValue").ShouldBeFalse();
}
