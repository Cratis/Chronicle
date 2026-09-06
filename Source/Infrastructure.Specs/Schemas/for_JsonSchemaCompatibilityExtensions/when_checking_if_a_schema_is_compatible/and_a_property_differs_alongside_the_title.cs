// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// Tolerating the title must not tolerate anything alongside it - a real shape change still needs a new
/// generation, whether or not the type was renamed at the same time (#3926).
/// </summary>
public class and_a_property_differs_alongside_the_title : Specification
{
    const string Stored = """{"type":"object","properties":{"width":{"type":"integer","format":"int32"}},"title":"ThingResized"}""";
    const string Generated = """{"type":"object","properties":{"width":{"type":"string"}},"title":"WidgetResized"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_not_consider_them_compatible() => _result.ShouldBeFalse();
}
