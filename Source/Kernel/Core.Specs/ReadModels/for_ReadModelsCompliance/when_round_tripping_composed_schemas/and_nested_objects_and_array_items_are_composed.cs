// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_nested_objects_and_array_items_are_composed : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "properties": {
            "profile": {
              "type": "object",
              "properties": { "localSecret": {{Pii}}, {{PublicProperties}} },
              "allOf": [{ "$ref": "#/$defs/Base" }]
            },
            "entries": {
              "type": "array",
              "items": {
                "type": "object",
                "allOf": [
                  { "$ref": "#/$defs/Base" },
                  { "type": "object", "properties": { "localSecret": {{Pii}}, {{PublicProperties}} } }
                ]
              }
            }
          },
          "$defs": { "Base": { "type": "object", "properties": { "baseSecret": {{Pii}} } } }
        }
        """;

    protected override ExpandoObject State() => CreateState(("profile", base.State()), ("entries", new[] { base.State(), base.State() }));

    Task Because() => RoundTrip();

    [Fact] void should_encrypt_the_nested_root_local_value() => IsEncrypted(Value(Profile(_stored), "localSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_nested_inherited_value() => IsEncrypted(Value(Profile(_stored), "baseSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_first_array_items_inherited_value() => IsEncrypted(Value(Entry(_stored, 0), "baseSecret")).ShouldBeTrue();
    [Fact] void should_encrypt_the_second_array_items_inherited_value() => IsEncrypted(Value(Entry(_stored, 1), "baseSecret")).ShouldBeTrue();
    [Fact] void should_release_the_active_nested_value() => Value(Profile(_released), "localSecret").ShouldEqual(PersonalValue);
    [Fact] void should_release_the_active_array_value() => Value(Entry(_released, 1), "baseSecret").ShouldEqual(PersonalValue);
    [Fact] void should_preserve_the_nested_explicit_null() => ((IDictionary<string, object?>)Profile(_stored)).ContainsKey("optionalStatus").ShouldBeTrue();
    [Fact] void should_preserve_the_array_items_explicit_null() => ((IDictionary<string, object?>)Entry(_stored, 1)).ContainsKey("optionalStatus").ShouldBeTrue();
    [Fact] void should_not_release_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_store_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_release_new_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterUpdate).ShouldBeFalse();
    [Fact] void should_release_json_without_losing_the_composed_schema() => _jsonReleaseError.ShouldBeNull();
    [Fact] void should_not_release_plaintext_as_json_after_erasure() => ContainsPersonalValue(_releasedJsonAfterUpdate).ShouldBeFalse();

    static ExpandoObject Profile(ExpandoObject instance) => (ExpandoObject)Value(instance, "profile")!;
    static ExpandoObject Entry(ExpandoObject instance, int index) => (ExpandoObject)((object[])Value(instance, "entries")!)[index];
}
