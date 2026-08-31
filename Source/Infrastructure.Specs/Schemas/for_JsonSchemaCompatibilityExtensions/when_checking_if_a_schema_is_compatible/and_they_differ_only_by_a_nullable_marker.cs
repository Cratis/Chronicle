// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// The nullability tolerance that predates the enumeration one still holds - a Chronicle upgrade can introduce the
/// marker on a schema stored before the marker existed.
/// </summary>
public class and_they_differ_only_by_a_nullable_marker : Specification
{
    const string Stored = """{"type":"object","properties":{"occurredAt":{"type":"string","format":"date-time-offset"}}}""";
    const string Generated = """{"type":"object","properties":{"occurredAt":{"type":"string","format":"date-time-offset?"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
