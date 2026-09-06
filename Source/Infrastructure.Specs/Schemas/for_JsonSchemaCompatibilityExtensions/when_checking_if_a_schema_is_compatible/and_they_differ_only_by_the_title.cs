// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3926 — the title carries the CLR record name, so
/// comparing it made renaming a record a breaking schema change even with its event type identifier pinned. It
/// describes nothing about the stored payload.
/// </summary>
public class and_they_differ_only_by_the_title : Specification
{
    const string Stored = """{"type":"object","properties":{"width":{"type":"integer","format":"int32"}},"required":["width"],"title":"ThingResized"}""";
    const string Generated = """{"type":"object","properties":{"width":{"type":"integer","format":"int32"}},"required":["width"],"title":"WidgetResized"}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
