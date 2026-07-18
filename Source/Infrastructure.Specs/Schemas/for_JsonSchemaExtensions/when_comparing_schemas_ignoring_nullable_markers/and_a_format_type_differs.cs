// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_comparing_schemas_ignoring_nullable_markers;

public class and_a_format_type_differs : Specification
{
    const string GuidFormat = """{"type":"object","properties":{"id":{"type":"string","format":"guid"}}}""";
    const string DateFormat = """{"type":"object","properties":{"id":{"type":"string","format":"date-time-offset"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJsonAsync(GuidFormat).GetAwaiter().GetResult()
        .EqualsIgnoringNullableFormatMarkers(JsonSchema.FromJsonAsync(DateFormat).GetAwaiter().GetResult());

    [Fact] void should_not_consider_them_equal() => _result.ShouldBeFalse();
}
