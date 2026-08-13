// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_string_is_supplied_for_an_integer_property : Specification
{
    record EventWithIntegerProperty(string Name, int Count);

    JsonSchema _schema;
    IList<JsonSchemaValidationError> _fromString;
    IList<JsonSchemaValidationError> _fromObject;

    void Establish() => _schema = JsonSchema.FromType<EventWithIntegerProperty>();

    void Because()
    {
        const string content = """{"name":"Mission","count":"not-an-integer"}""";
        _fromString = _schema.Validate(content);
        _fromObject = _schema.Validate(System.Text.Json.Nodes.JsonNode.Parse(content)!.AsObject());
    }

    [Fact] void should_report_a_single_error() => _fromString.Count.ShouldEqual(1);
    [Fact] void should_report_a_wrong_property_type() => _fromString.Single().Kind.ShouldEqual(JsonSchemaValidationErrorKind.WrongPropertyType);
    [Fact] void should_report_the_offending_property_path() => _fromString.Single().Path.ShouldEqual("count");
    [Fact] void should_agree_across_both_overloads() =>
        _fromObject.Select(_ => (_.Path, _.Kind)).ShouldContainOnly(_fromString.Select(_ => (_.Path, _.Kind)));
}
