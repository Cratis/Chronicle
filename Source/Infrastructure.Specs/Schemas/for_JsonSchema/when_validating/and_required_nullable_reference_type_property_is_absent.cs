// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_required_nullable_reference_type_property_is_absent : Specification
{
    record EventWithNullableComment(string Name, string? Comment);

    JsonSchema _schema;
    IList<JsonSchemaValidationError> _result;

    void Establish() => _schema = JsonSchema.FromType<EventWithNullableComment>();

    void Because()
    {
        var json = JsonSerializer.Serialize(new { name = "Test" });
        _result = _schema.Validate(json);
    }

    [Fact] void should_not_report_any_errors() => _result.ShouldBeEmpty();
}
