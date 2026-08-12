// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_property_has_no_declared_type : Specification
{
    const string SchemaJson = """{"type":"object","properties":{"payload":{}}}""";

    IList<JsonSchemaValidationError> _forANumber;
    IList<JsonSchemaValidationError> _forAString;
    IList<JsonSchemaValidationError> _forNull;
    IList<JsonSchemaValidationError> _forAnObject;

    void Because()
    {
        var schema = JsonSchema.FromJson(SchemaJson);
        _forANumber = schema.Validate("""{"payload":42}""");
        _forAString = schema.Validate("""{"payload":"text"}""");
        _forNull = schema.Validate("""{"payload":null}""");
        _forAnObject = schema.Validate("""{"payload":{"anything":true}}""");
    }

    [Fact] void should_accept_a_number() => _forANumber.ShouldBeEmpty();
    [Fact] void should_accept_a_string() => _forAString.ShouldBeEmpty();
    [Fact] void should_accept_null() => _forNull.ShouldBeEmpty();
    [Fact] void should_accept_an_object() => _forAnObject.ShouldBeEmpty();
}
