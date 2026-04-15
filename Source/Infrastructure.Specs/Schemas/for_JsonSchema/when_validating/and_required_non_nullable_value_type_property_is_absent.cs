// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_required_non_nullable_value_type_property_is_absent : Specification
{
    record EventWithRequiredDate(string Name, DateOnly StartDate);

    JsonSchema _schema;
    IList<JsonSchemaValidationError> _result;

    void Establish() => _schema = JsonSchema.FromType<EventWithRequiredDate>();

    void Because()
    {
        var json = JsonSerializer.Serialize(new { name = "Test" });
        _result = _schema.Validate(json);
    }

    [Fact] void should_report_one_error() => _result.Count.ShouldEqual(1);
    [Fact] void should_report_property_required_error() => _result[0].Kind.ShouldEqual(JsonSchemaValidationErrorKind.PropertyRequired);
    [Fact] void should_report_error_for_start_date() => _result[0].Path.ShouldEqual("startDate");
}
