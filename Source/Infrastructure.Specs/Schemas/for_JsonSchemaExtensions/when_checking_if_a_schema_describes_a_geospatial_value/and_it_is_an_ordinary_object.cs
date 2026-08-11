// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_checking_if_a_schema_describes_a_geospatial_value;

/// <summary>
/// A schema can declare no members and still not be a typed value — a dictionary, a base type kept open for
/// derived payloads, a reference that resolves to nothing. Those keep failing where a compliance walk meets
/// them rather than being waved through as values nothing can be classified inside.
/// </summary>
public class and_it_is_an_ordinary_object : Specification
{
    const string WithProperties = """{"type":"object","properties":{"city":{"type":"string"}}}""";
    const string Dictionary = """{"type":"object","additionalProperties":{"type":"string"}}""";
    const string OpenObject = """{"type":"object","title":"Shape"}""";
    const string UnresolvableReference = """{"type":"object","$ref":"#/$defs/NotThere"}""";

    [Fact] void should_not_describe_a_geospatial_value_when_it_declares_properties() => JsonSchema.FromJson(WithProperties).DescribesGeospatialValue().ShouldBeFalse();
    [Fact] void should_not_describe_a_geospatial_value_for_a_dictionary() => JsonSchema.FromJson(Dictionary).DescribesGeospatialValue().ShouldBeFalse();
    [Fact] void should_not_describe_a_geospatial_value_for_an_open_object() => JsonSchema.FromJson(OpenObject).DescribesGeospatialValue().ShouldBeFalse();
    [Fact] void should_not_describe_a_geospatial_value_for_an_unresolvable_reference() => JsonSchema.FromJson(UnresolvableReference).DescribesGeospatialValue().ShouldBeFalse();
}
