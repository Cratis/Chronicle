// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes.for_SchemaPaths;

public class when_checking_declaration : Specification
{
    const string Schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"},\"address\":{\"type\":\"object\",\"properties\":{\"city\":{\"type\":\"string\"}}},\"tags\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"properties\":{\"label\":{\"type\":\"string\"}}}},\"scalar\":{\"type\":\"string\"}}}";

    JsonSchema _schema;

    void Establish() => _schema = JsonSchema.FromJsonAsync(Schema).GetAwaiter().GetResult();

    [Fact] void should_declare_a_top_level_property() => SchemaPaths.Declares(_schema, "name").ShouldBeTrue();
    [Fact] void should_not_declare_a_missing_property() => SchemaPaths.Declares(_schema, "missing").ShouldBeFalse();
    [Fact] void should_declare_a_nested_property() => SchemaPaths.Declares(_schema, "address.city").ShouldBeTrue();
    [Fact] void should_not_declare_a_missing_nested_property() => SchemaPaths.Declares(_schema, "address.zip").ShouldBeFalse();
    [Fact] void should_declare_a_property_within_array_elements() => SchemaPaths.Declares(_schema, "tags[].label").ShouldBeTrue();
    [Fact] void should_not_declare_elements_of_a_non_array() => SchemaPaths.Declares(_schema, "scalar[].label").ShouldBeFalse();
}
