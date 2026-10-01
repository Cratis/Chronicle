// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_multiple_inline_groups_have_personal_values : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "allOf": [
            { "type": "object", "properties": { "localSecret": {{Pii}}, {{PublicProperties}} } },
            { "type": "object", "properties": { "baseSecret": {{Pii}} } }
          ]
        }
        """;

    Task Because() => RoundTrip();

    [Fact] void should_encrypt_the_first_group() => IsEncrypted(Value(_stored, "localSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_second_group() => IsEncrypted(Value(_stored, "baseSecret")).ShouldBeTrue();
    [Fact] void should_release_the_active_second_group() => Value(_released, "baseSecret").ShouldEqual(PersonalValue);
    [Fact] void should_not_release_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_store_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_release_new_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterUpdate).ShouldBeFalse();
    [Fact] void should_not_release_plaintext_as_json_after_erasure() => ContainsPersonalValue(_releasedJsonAfterUpdate).ShouldBeFalse();
}
