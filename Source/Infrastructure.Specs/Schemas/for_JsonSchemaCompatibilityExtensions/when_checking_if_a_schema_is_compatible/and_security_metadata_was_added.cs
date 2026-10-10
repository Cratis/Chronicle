// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaCompatibilityExtensions.when_checking_if_a_schema_is_compatible;

public class and_security_metadata_was_added : Specification
{
    bool _result;

    void Because() => _result = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string"}}}""")
        .IsCompatibleWith(JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string","security":[{"metadataType":"EncryptedNamespace","details":""}]}}}"""));

    [Fact] void should_accept_the_added_metadata() => _result.ShouldBeTrue();
}
