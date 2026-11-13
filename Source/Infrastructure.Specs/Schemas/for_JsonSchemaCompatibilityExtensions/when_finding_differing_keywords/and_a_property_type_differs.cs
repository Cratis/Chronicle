// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_finding_differing_keywords;

public class and_a_property_type_differs : Specification
{
    const string Stored = """{"type":"object","properties":{"width":{"type":"integer"}},"title":"A"}""";
    const string Generated = """{"type":"object","properties":{"width":{"type":"string"}},"title":"B"}""";

    SchemaDifference? _result;
    bool _compatible;

    void Because()
    {
        _result = JsonSchema.FromJson(Stored).DifferingKeywords(JsonSchema.FromJson(Generated));
        _compatible = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));
    }

    [Fact] void should_point_at_the_property() => _result!.Path.ShouldEqual("$/properties/width");
    [Fact] void should_name_the_differing_keyword() => _result!.Keyword.ShouldEqual("type");
    [Fact] void should_agree_with_the_compatibility_check() => _compatible.ShouldBeFalse();
}
