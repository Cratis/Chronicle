// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

public class and_compliance_was_removed : Specification
{
    bool _result;

    void Because() => _result = JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null,"compliance":[{"metadataType":"PII","details":""}]}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal","default":null}}}"""));

    [Fact] void should_refuse_the_removal() => _result.ShouldBeFalse();
}
