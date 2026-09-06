// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

/// <summary>
/// An enumeration is as likely to sit behind a <c>$defs</c> entry or inside a nested object as it is to sit directly
/// on a root property, and it grows there for exactly the same reasons.
/// </summary>
public class and_a_nested_enumeration_gained_a_member : Specification
{
    const string Stored =
        """
        {"type":"object","$defs":{"Status":{"type":"integer","enum":[0,1],"x-enumNames":["Unknown","Verified"]}},"properties":{"details":{"type":"object","properties":{"status":{"$ref":"#/$defs/Status"}}}}}
        """;

    const string Generated =
        """
        {"type":"object","$defs":{"Status":{"type":"integer","enum":[0,1,2],"x-enumNames":["Unknown","Verified","Revoked"]}},"properties":{"details":{"type":"object","properties":{"status":{"$ref":"#/$defs/Status"}}}}}
        """;

    bool _result;

    void Because() => _result = JsonSchema.FromJson(Stored).IsCompatibleWith(JsonSchema.FromJson(Generated));

    [Fact] void should_consider_them_compatible() => _result.ShouldBeTrue();
}
