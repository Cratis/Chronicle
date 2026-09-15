// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// A member that disappears leaves every event already stored with that value denoting nothing, which is a change to
/// the meaning of history and therefore needs a new generation with a value map saying what those values became.
/// </summary>
public class and_an_enumeration_member_was_removed : Specification
{
    const string Stored = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1,2],"x-enumNames":["Unknown","Verified","Revoked"]}}}""";
    const string Generated = """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}}}""";

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_not_consider_them_compatible() => _result.ShouldBeFalse();
}
