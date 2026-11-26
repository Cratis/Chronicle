// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_many_protected_values_are_reported_in_any_order : given.an_append_intent
{
    StoredAppendFingerprint _inOrder;
    StoredAppendFingerprint _shuffled;
    EncryptionKey _otherKey;

    void Establish()
    {
        _otherKey = new([7, 8, 9], [10, 11, 12]);
        _input = _input with { Content = JsonNode.Parse("""{"a":"x","b":{"n":2},"c":"z","d":"plain"}""")!.AsObject() };
    }

    void Because()
    {
        var locations = Locations("a", "b", "c");
        var reversed = Locations("c", "b", "a");
        _inOrder = Compute(_input with { ProtectedValues = locations }, Use("a", ProtectionKind.Pii, _key), Use("b", ProtectionKind.Pii, _key), Use("c", ProtectionKind.EncryptedSubject, _otherKey));
        _shuffled = Compute(_input with { ProtectedValues = reversed }, Use("c", ProtectionKind.EncryptedSubject, _otherKey), Use("b", ProtectionKind.Pii, _key), Use("a", ProtectionKind.Pii, _key));
    }

    [Fact] void should_compute_the_same_plain_digest() => _shuffled.Plain.ShouldEqual(_inOrder.Plain);
    [Fact] void should_report_one_part_per_kind() => _inOrder.ProtectedParts.Count.ShouldEqual(2);
    [Fact] void should_compute_the_same_pii_hash() => _shuffled.ProtectedParts[0].Hash.ShouldEqual(_inOrder.ProtectedParts[0].Hash);
    [Fact] void should_compute_the_same_encrypted_hash() => _shuffled.ProtectedParts[1].Hash.ShouldEqual(_inOrder.ProtectedParts[1].Hash);
    [Fact] void should_pin_the_plain_digest() => _inOrder.Plain.ShouldEqual("115e3525ce33e7f8eb3bad571e7c378f2f7970cfd009bedebae3653df4030218");
    [Fact] void should_pin_the_pii_hash() => _inOrder.ProtectedParts[0].Hash.ShouldEqual("75f0cef01cebc69bef9c8f3bd0945e6b294bbe0c00275688b12df24a0ae31347");
    [Fact] void should_pin_the_encrypted_hash() => _inOrder.ProtectedParts[1].Hash.ShouldEqual("ab5ad0f04ec4c58430561254bf7c162cdcc3795220f4d7ae545390d8e886dcd9");

    ProtectedValueLocation[] Locations(params string[] paths) =>
        [.. paths.Select(path => new ProtectedValueLocation(path, path == "c" ? ProtectionKind.EncryptedSubject : ProtectionKind.Pii, "subject", _input.Content[path]!.DeepClone()))];
}
