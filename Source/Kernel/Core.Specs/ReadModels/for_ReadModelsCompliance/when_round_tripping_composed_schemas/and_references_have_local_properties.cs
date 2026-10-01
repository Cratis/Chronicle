// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_references_have_local_properties : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "properties": {
            "profile": {
              "$ref": "#/$defs/Base",
              "properties": { "localSecret": {{Pii}}, {{PublicProperties}} }
            }
          },
          "$defs": {
            "Base": {
              "type": "object",
              "allOf": [
                { "type": "object", "properties": { "baseSecret": {{Pii}} } },
                { "type": "object", "properties": { "otherSecret": {{Pii}} } }
              ]
            }
          }
        }
        """;

    protected override ExpandoObject State() => CreateState(("profile", CreateState(("localSecret", PersonalValue), ("baseSecret", PersonalValue), ("otherSecret", PersonalValue), ("status", "updated"), ("optionalStatus", null))));

    Task Because() => RoundTrip();

    [Fact] void should_encrypt_the_reference_sibling() => IsEncrypted(Value(Profile(_stored), "localSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_first_referenced_group() => IsEncrypted(Value(Profile(_stored), "baseSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_second_referenced_group() => IsEncrypted(Value(Profile(_stored), "otherSecret")).ShouldBeTrue();
    [Fact] void should_release_the_active_reference_sibling() => Value(Profile(_released), "localSecret").ShouldEqual(PersonalValue);
    [Fact] void should_release_the_active_referenced_group() => Value(Profile(_released), "otherSecret").ShouldEqual(PersonalValue);
    [Fact] void should_not_release_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_store_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_release_json_without_schema_drift() => _jsonReleaseError.ShouldBeNull();
    [Fact] void should_not_release_plaintext_as_json_after_erasure() => ContainsPersonalValue(_releasedJsonAfterUpdate).ShouldBeFalse();

    static ExpandoObject Profile(ExpandoObject instance) => (ExpandoObject)Value(instance, "profile")!;
}
