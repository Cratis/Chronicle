// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_root_properties_extend_a_referenced_base : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "properties": { "localSecret": {{Pii}}, {{PublicProperties}} },
          "allOf": [{ "$ref": "#/$defs/Base" }],
          "$defs": { "Base": { "type": "object", "properties": { "baseSecret": {{Pii}} } } }
        }
        """;

    Task Because() => RoundTrip();

    [Fact] void should_encrypt_the_root_local_value() => IsEncrypted(Value(_stored, "localSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_inherited_value() => IsEncrypted(Value(_stored, "baseSecret")).ShouldBeTrue();
    [Fact] void should_release_the_active_root_local_value() => Value(_released, "localSecret").ShouldEqual(PersonalValue);
    [Fact] void should_release_the_active_inherited_value() => Value(_released, "baseSecret").ShouldEqual(PersonalValue);
    [Fact] void should_not_release_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_store_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_release_new_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterUpdate).ShouldBeFalse();
    [Fact] void should_not_release_plaintext_as_json_after_erasure() => ContainsPersonalValue(_releasedJsonAfterUpdate).ShouldBeFalse();
    [Fact] void should_keep_the_non_personal_update() => Value(_storedAfterErasure, "status").ShouldEqual("updated");
    [Fact] async Task should_not_recreate_the_key() => (await _keys.HasFor("store", "Default", Subject)).ShouldBeFalse();
}
