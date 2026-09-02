// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// The underlying value is what a stored payload carries, so renaming a member changes nothing about what history
/// says - and an enumeration mirroring an external system gets renamed on that system's schedule, not the owning
/// application's.
/// </summary>
public class and_an_enumeration_member_was_renamed : Specification
{
    const string Stored = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";
    const string Generated = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unspecified","Confirmed"]}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
