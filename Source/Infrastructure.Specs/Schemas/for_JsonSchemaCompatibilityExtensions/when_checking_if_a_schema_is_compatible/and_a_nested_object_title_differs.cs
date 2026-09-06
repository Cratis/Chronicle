// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// A value object nested inside an event carries a title of its own once it is generated as a named schema, and
/// renaming that record is no more of a payload change than renaming the event's own (#3926).
/// </summary>
public class and_a_nested_object_title_differs : Specification
{
    const string Stored = """{"type":"object","properties":{"price":{"type":"object","title":"PriceV1","properties":{"amount":{"type":"number"}}}}}""";
    const string Generated = """{"type":"object","properties":{"price":{"type":"object","title":"Price","properties":{"amount":{"type":"number"}}}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
