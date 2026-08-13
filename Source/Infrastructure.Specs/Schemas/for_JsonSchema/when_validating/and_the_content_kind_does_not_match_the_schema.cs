// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_the_content_kind_does_not_match_the_schema : Specification
{
    record EventWithNonNullableProperty(string Name, DateOnly StartDate);

    IList<JsonSchemaValidationError> _objectSchemaGivenAString;
    IList<JsonSchemaValidationError> _stringSchemaGivenAnObjectFromString;
    IList<JsonSchemaValidationError> _stringSchemaGivenAnObjectFromObject;

    void Because()
    {
        _objectSchemaGivenAString = JsonSchema.FromType<EventWithNonNullableProperty>().Validate("\"just-a-string\"");

        var stringSchema = JsonSchema.FromJson("""{"type":"string"}""");
        const string content = "{}";
        _stringSchemaGivenAnObjectFromString = stringSchema.Validate(content);
        _stringSchemaGivenAnObjectFromObject = stringSchema.Validate(System.Text.Json.Nodes.JsonNode.Parse(content)!.AsObject());
    }

    [Fact] void should_reject_a_string_for_an_object_schema() =>
        _objectSchemaGivenAString.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);

    [Fact] void should_report_the_root_with_no_path() => _objectSchemaGivenAString.Single().Path.ShouldBeNull();

    [Fact] void should_reject_an_object_for_a_string_schema() =>
        _stringSchemaGivenAnObjectFromString.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);

    [Fact] void should_agree_across_both_overloads() =>
        _stringSchemaGivenAnObjectFromObject.Select(_ => (_.Path, _.Kind))
            .ShouldContainOnly(_stringSchemaGivenAnObjectFromString.Select(_ => (_.Path, _.Kind)));
}
