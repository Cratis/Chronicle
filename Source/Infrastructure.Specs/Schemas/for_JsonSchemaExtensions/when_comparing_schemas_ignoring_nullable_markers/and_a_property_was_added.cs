// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_comparing_schemas_ignoring_nullable_markers;

public class and_a_property_was_added : Specification
{
    const string Original = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string WithAddedProperty = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJsonAsync(Original).GetAwaiter().GetResult()
        .EqualsIgnoringNullableFormatMarkers(JsonSchema.FromJsonAsync(WithAddedProperty).GetAwaiter().GetResult());

    [Fact] void should_not_consider_them_equal() => _result.ShouldBeFalse();
}
