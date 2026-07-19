// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_comparing_schemas_ignoring_nullable_markers;

public class and_they_differ_only_by_a_marker : Specification
{
    const string WithoutMarker = """{"type":"object","properties":{"occurredAt":{"type":"string","format":"date-time-offset"}}}""";
    const string WithMarker = """{"type":"object","properties":{"occurredAt":{"type":"string","format":"date-time-offset?"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJsonAsync(WithoutMarker).GetAwaiter().GetResult()
        .EqualsIgnoringNullableFormatMarkers(JsonSchema.FromJsonAsync(WithMarker).GetAwaiter().GetResult());

    [Fact] void should_consider_them_equal() => _result.ShouldBeTrue();
}
