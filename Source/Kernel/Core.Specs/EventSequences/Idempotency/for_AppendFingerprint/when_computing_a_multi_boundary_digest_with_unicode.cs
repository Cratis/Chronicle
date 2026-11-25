// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_computing_a_multi_boundary_digest_with_unicode : given.an_append_intent
{
    const string Content = """{"nested":{"name":"é😀"},"items":[null,{"β":"雪"}],"plain":"é"}""";
    StoredAppendFingerprint _reordered;
    ProtectedValueUse[] _uses;

    void Establish()
    {
        _input = _input with
        {
            Content = JsonNode.Parse(Content)!.AsObject(),
            ProtectedValues =
            [
                new("nested.name", ProtectionKind.Pii, "subject", JsonValue.Create("é😀")),
                new("items[0]", ProtectionKind.Pii, "subject", null!),
                new("items[1]", ProtectionKind.EncryptedSubject, "subject", JsonNode.Parse("""{"β":"雪"}"""))
            ]
        };
        _uses = [.. _input.ProtectedValues.Select(location => new ProtectedValueUse(
            location.Path,
            location.Kind,
            location.Plaintext?.DeepClone()!,
            new(location.Kind, location.Identifier, location.Kind == ProtectionKind.Pii ? _key : new EncryptionKey([7, 8, 9], [10, 11, 12]), new EncryptionKeyRevision(3))))];
    }

    void Because()
    {
        _result = Compute(_input, _uses);
        _reordered = Compute(
            _input with
            {
                Content = JsonNode.Parse("""{"plain":"é","items":[null,{"β":"雪"}],"nested":{"name":"é😀"}}""")!.AsObject(),
                ProtectedValues = _input.ProtectedValues.Reverse().ToArray()
            },
            _uses.Reverse().ToArray());
    }

    [Fact] void should_pin_the_plain_digest() => _result.Plain.ShouldEqual("31b8dc90e8c4c59ac0f655af9f09a9078cf9c9bdad9d6ce3a452cb21803060bf");
    [Fact] void should_pin_the_pii_digest() => _result.ProtectedParts[0].Hash.ShouldEqual("8d9dad853efe409475d5a9f0e9321fcdde0abb08d6ae2ce45fb9e7ffd59a4835");
    [Fact] void should_pin_the_encrypted_digest() => _result.ProtectedParts[1].Hash.ShouldEqual("f451fed4bc3ea89b37675b159ae0bea34f0cc087415ffd64161f6c178773648d");
    [Fact] void should_order_the_parts_by_kind() => _result.ProtectedParts.Select(part => part.Kind).ShouldEqual([ProtectionKind.Pii, ProtectionKind.EncryptedSubject]);
    [Fact] void should_pin_the_key_references() => _result.ProtectedParts.Select(part => part.KeyReference).ShouldEqual(["039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81", "66a6757151f8ee55db127716c7e3dce0be8074b64e20eda542e5c1e46ca9c41e"]);
    [Fact] void should_pin_the_revisions() => _result.ProtectedParts.Select(part => part.Revision.Value).ShouldEqual([3u, 3u]);
    [Fact] void should_ignore_property_and_report_order() => _reordered.Format().ShouldEqual(_result.Format());
    [Fact] void should_not_mutate_the_plaintext() => _input.Content.ToJsonString().ShouldEqual(JsonNode.Parse(Content)!.ToJsonString());
}
