// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchema.when_validating;

public class and_a_number_is_supplied_for_an_integer_property : Specification
{
    const string SchemaJson = """{"type":"object","properties":{"count":{"type":"integer"},"ratio":{"type":"number"}}}""";

    IList<JsonSchemaValidationError> _forAWholeValuedNumber;
    IList<JsonSchemaValidationError> _forAnIntegerAgainstNumber;
    IList<JsonSchemaValidationError> _forAFractionalNumber;

    void Because()
    {
        var schema = JsonSchema.FromJson(SchemaJson);
        _forAWholeValuedNumber = schema.Validate("""{"count":3.0}""");
        _forAnIntegerAgainstNumber = schema.Validate("""{"ratio":3}""");
        _forAFractionalNumber = schema.Validate("""{"count":3.5}""");
    }

    [Fact] void should_accept_a_whole_valued_number_for_an_integer() => _forAWholeValuedNumber.ShouldBeEmpty();
    [Fact] void should_accept_an_integer_for_a_number() => _forAnIntegerAgainstNumber.ShouldBeEmpty();
    [Fact] void should_reject_a_fractional_number_for_an_integer() => _forAFractionalNumber.Single().Path.ShouldEqual("count");
}
