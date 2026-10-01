// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_round_tripping_composed_schemas;

public class and_properties_reference_an_enum : given.a_composed_read_model
{
    protected override string SchemaJson => $$"""
        {
          "type": "object",
          "properties": {
            "localSecret": {{Pii}},
            "privateKind": { "$ref": "#/$defs/Kind", "compliance": [{ "metadataType": "PII", "details": "" }] },
            "publicKind": { "$ref": "#/$defs/Kind" },
            "privateInlineKind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Unknown", "Home"], "compliance": [{ "metadataType": "PII", "details": "" }] },
            "publicInlineKind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Unknown", "Home"] }
          },
          "$defs": { "Kind": { "type": "integer", "enum": [0, 1], "x-enumNames": ["Unknown", "Home"] } }
        }
        """;

    protected override ExpandoObject State() => CreateState(("localSecret", PersonalValue), ("privateKind", Kind.Home), ("publicKind", Kind.Home), ("privateInlineKind", Kind.Home), ("publicInlineKind", Kind.Home));

    Task Because() => RoundTrip();

    [Fact] void should_encrypt_the_referenced_personal_enum() => IsEncrypted(Value(_stored, "privateKind")).ShouldBeTrue();
    [Fact] void should_store_the_public_enum_in_its_converted_type() => Value(_stored, "publicKind").ShouldEqual(1);
    [Fact] void should_encrypt_the_inline_personal_enum() => IsEncrypted(Value(_stored, "privateInlineKind")).ShouldBeTrue();
    [Fact] void should_store_the_inline_public_enum_in_its_converted_type() => Value(_stored, "publicInlineKind").ShouldEqual(1);
    [Fact] void should_release_the_active_inline_personal_enum() => Value(_released, "privateInlineKind").ShouldEqual(1);
    [Fact] void should_release_the_active_inline_public_enum() => Value(_released, "publicInlineKind").ShouldEqual(1);
    [Fact] void should_release_the_active_personal_enum() => Value(_released, "privateKind").ShouldEqual(1);
    [Fact] void should_release_the_active_public_enum() => Value(_released, "publicKind").ShouldEqual(1);
    [Fact] void should_erase_the_stored_personal_enum() => Value(_storedAfterErasure, "privateKind").ShouldEqual(0);
    [Fact] void should_not_release_the_personal_enum_after_erasure() => Value(_releasedAfterErasure, "privateKind").ShouldEqual(0);
    [Fact] void should_not_release_new_personal_enum_values_after_erasure() => Value(_releasedAfterUpdate, "privateKind").ShouldEqual(0);
    [Fact] void should_release_the_public_enum_after_erasure() => Value(_releasedAfterUpdate, "publicKind").ShouldEqual(1);

    enum Kind
    {
        Unknown = 0,
        Home = 1
    }
}
