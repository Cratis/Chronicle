// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_finding_differing_keywords;

public class and_only_annotations_differ : Specification
{
    const string Stored = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string Generated = """{"type":"object","title":"T","description":"d","$comment":"c","examples":[1],"properties":{"name":{"type":"string","deprecated":true,"contentEncoding":"base64"}}}""";

    SchemaDifference? _result;

    void Because() => _result = JsonSchema.FromJson(Stored).DifferingKeywords(JsonSchema.FromJson(Generated));

    [Fact] void should_find_no_difference() => _result.ShouldBeNull();
}
