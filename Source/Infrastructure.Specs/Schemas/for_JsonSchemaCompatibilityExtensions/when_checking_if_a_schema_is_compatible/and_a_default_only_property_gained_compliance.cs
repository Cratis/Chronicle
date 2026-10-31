// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

public class and_a_default_only_property_gained_compliance : Specification
{
    bool _result;

    void Because() => _result = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"default":null}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null,"compliance":[{"metadataType":"PII","details":""}]}}}"""));

    [Fact] void should_accept_the_added_metadata() => _result.ShouldBeTrue();
}
