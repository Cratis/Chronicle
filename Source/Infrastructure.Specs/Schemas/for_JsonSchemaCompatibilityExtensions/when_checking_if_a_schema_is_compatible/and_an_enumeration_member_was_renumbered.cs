// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// Renumbering keeps the names but moves them onto different underlying values, so every event already stored now
/// reads as a different member. That is the one enumeration change a value map exists for, and it needs a new
/// generation to carry it.
/// </summary>
public class and_an_enumeration_member_was_renumbered : Specification
{
    const string Stored = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";
    const string Generated = """{"type":"object","properties":{"status":{"type":"integer","enum":[10,11],"x-enumNames":["Unknown","Verified"]}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_not_consider_them_compatible() => _result.ShouldBeFalse();
}
