// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// A member added to an enumeration leaves every stored value denoting exactly what it denoted before, so the
/// enumeration is free to grow without a new generation.
/// </summary>
public class and_an_enumeration_gained_a_member : Specification
{
    const string Stored = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";
    const string Generated = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1,2],"x-enumNames":["Unknown","Verified","Revoked"]}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
