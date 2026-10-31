// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

public class and_a_default_only_property_gained_type_and_a_different_default : Specification
{
    bool _result;

    void Because() => _result = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":1}}}"""));

    [Fact] void should_refuse_the_changed_default() => _result.ShouldBeFalse();
}
