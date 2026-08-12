// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_null_is_supplied_for_a_non_nullable_property : Specification
{
    record EventWithNonNullableProperty(string Name, int Count);

    IList<JsonSchemaValidationError> _result;

    void Because() => _result = JsonSchema.FromType<EventWithNonNullableProperty>().Validate("""{"name":"Mission","count":null}""");

    [Fact] void should_report_a_single_error() => _result.Count.ShouldEqual(1);
    [Fact] void should_report_a_wrong_property_type() => _result.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);
    [Fact] void should_report_the_offending_property_path() => _result.Single().Path.ShouldEqual("count");
}
