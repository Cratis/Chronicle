// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_names_differ_only_by_case : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "properties": { "name": { "type": "string" }, "Name": {{Pii}} }
        }
        """;

    protected override ExpandoObject State() => CreateState(("name", "public-name"), ("Name", PersonalValue));

    Task Because() => RoundTrip();

    [Fact] void should_not_overwrite_the_public_value_with_personal_plaintext() => Value(_stored, "name").ShouldEqual("public-name");
    [Fact] void should_encrypt_the_exact_case_personal_value() => IsEncrypted(Value(_stored, "Name")).ShouldBeTrue();
    [Fact] void should_release_the_active_personal_value() => Value(_released, "Name").ShouldEqual(PersonalValue);
    [Fact] void should_not_release_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_store_plaintext_after_erasure() => ContainsPersonalValue(_storedAfterErasure).ShouldBeFalse();
    [Fact] void should_not_release_new_plaintext_after_erasure() => ContainsPersonalValue(_releasedAfterUpdate).ShouldBeFalse();
}
