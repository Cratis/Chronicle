// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_getting_schema_for_property_path;

public class with_camel_case_segment_in_pascal_case_schema : Specification
{
    record Address(string Street, string City);
    record PersonWithAddress(string Name, Address HomeAddress);

    JsonSchema _schema;
    JsonSchema _result;

    void Establish() =>
        _schema = JsonSchema.FromType(typeof(PersonWithAddress), new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        });

    void Because() => _result = _schema.GetSchemaForPropertyPath(new PropertyPath("homeAddress.street"));

    [Fact] void should_resolve_to_string_type_schema() => _result.Type.ShouldEqual(JsonObjectType.String);
}
