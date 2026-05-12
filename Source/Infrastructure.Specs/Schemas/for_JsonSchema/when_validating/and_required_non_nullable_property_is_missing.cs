// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_required_non_nullable_property_is_missing : Specification
{
    record EventWithNonNullableProperty(string Name, DateOnly StartDate);

    IList<JsonSchemaValidationError> _result;

    void Because()
    {
        var schema = JsonSchema.FromType<EventWithNonNullableProperty>();
        _result = schema.Validate("""{"name":"Mission"}""");
    }

    [Fact] void should_report_required_property_error() => _result.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.PropertyRequired);
    [Fact] void should_report_missing_property_name() => _result.Single().Path.ShouldEqual("startDate");
}
