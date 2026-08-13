// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_null_is_supplied_for_a_nullable_property : Specification
{
    record EventWithNullableProperty(string Name, DateOnly? StartDate);

    IList<JsonSchemaValidationError> _fromGeneratedSchema;
    IList<JsonSchemaValidationError> _fromTypeArrayNullMarker;
    IList<JsonSchemaValidationError> _fromFormatNullMarker;

    void Because()
    {
        _fromGeneratedSchema = JsonSchema.FromType<EventWithNullableProperty>().Validate("""{"name":"Mission","startDate":null}""");
        _fromTypeArrayNullMarker = JsonSchema.FromJson("""{"type":"object","properties":{"startDate":{"type":["string","null"]}}}""")
            .Validate("""{"startDate":null}""");
        _fromFormatNullMarker = JsonSchema.FromJson("""{"type":"object","properties":{"startDate":{"type":"string","format":"date?"}}}""")
            .Validate("""{"startDate":null}""");
    }

    [Fact] void should_accept_null_on_the_generated_schema() => _fromGeneratedSchema.ShouldBeEmpty();
    [Fact] void should_accept_null_declared_in_the_type() => _fromTypeArrayNullMarker.ShouldBeEmpty();
    [Fact] void should_accept_null_marked_on_the_format() => _fromFormatNullMarker.ShouldBeEmpty();
}
