// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_finding_differing_keywords;

public class and_an_enumeration_member_was_removed : Specification
{
    const string Stored = """{"type":"object","properties":{"kind":{"type":"integer","enum":[0,1,2]}}}""";
    const string Generated = """{"type":"object","properties":{"kind":{"type":"integer","enum":[0,1]}}}""";

    SchemaDifference? _result;

    void Because() => _result = JsonSchema.FromJson(Stored).DifferingKeywords(JsonSchema.FromJson(Generated));

    [Fact] void should_point_at_the_property() => _result!.Path.ShouldEqual("$/properties/kind");
    [Fact] void should_name_the_enumeration() => _result!.Keyword.ShouldEqual("enum");
}
