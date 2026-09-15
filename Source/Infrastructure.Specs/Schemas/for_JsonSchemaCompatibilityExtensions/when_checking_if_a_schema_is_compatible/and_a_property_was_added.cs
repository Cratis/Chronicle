// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// Everything outside the enumeration and nullability tolerances stays a breaking change - a property added to a
/// generation that is already storing events still needs a new generation and a migration.
/// </summary>
public class and_a_property_was_added : Specification
{
    const string Stored = """{"type":"object","properties":{"name":{"type":"string"}}}""";
    const string Generated = """{"type":"object","properties":{"name":{"type":"string"},"age":{"type":"integer"}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_not_consider_them_compatible() => _result.ShouldBeFalse();
}
