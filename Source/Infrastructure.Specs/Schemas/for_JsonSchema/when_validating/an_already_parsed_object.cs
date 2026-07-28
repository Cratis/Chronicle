// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class an_already_parsed_object : Specification
{
    record EventWithNonNullableProperty(string Name, DateOnly StartDate);

    JsonSchema _schema;
    IList<JsonSchemaValidationError> _fromObjectMissingRequired;
    IList<JsonSchemaValidationError> _fromStringMissingRequired;
    IList<JsonSchemaValidationError> _fromObjectValid;

    void Establish() => _schema = JsonSchema.FromType<EventWithNonNullableProperty>();

    void Because()
    {
        const string missingRequired = """{"name":"Mission"}""";
        _fromObjectMissingRequired = _schema.Validate(JsonNode.Parse(missingRequired)!.AsObject());
        _fromStringMissingRequired = _schema.Validate(missingRequired);
        _fromObjectValid = _schema.Validate(JsonNode.Parse("""{"name":"Mission","startDate":"2026-01-01"}""")!.AsObject());
    }

    [Fact] void should_report_the_missing_required_property() =>
        _fromObjectMissingRequired.Single().Path.ShouldEqual("startDate");

    [Fact] void should_match_the_string_overload_for_missing_required() =>
        _fromObjectMissingRequired.Select(_ => (_.Path, _.Kind))
            .ShouldContainOnly(_fromStringMissingRequired.Select(_ => (_.Path, _.Kind)));

    [Fact] void should_report_no_errors_for_valid_content() =>
        _fromObjectValid.ShouldBeEmpty();
}
