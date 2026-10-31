// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

public class and_a_typed_property_regressed_to_default_only : Specification
{
    bool _result;

    void Because() => _result = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":["number","null"],"format":"decimal?","default":null}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}"""));

    [Fact] void should_accept_a_legacy_client() => _result.ShouldBeTrue();
}
