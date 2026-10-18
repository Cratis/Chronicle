// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_protected_properties_are_reordered : given.an_append_intent
{
    ProtectedValueUse[] _uses;
    void Establish()
    {
        _input = _input with { Content = JsonNode.Parse("""{"profile":{"name":"Alice"},"items":[{"secret":{"b":2,"a":1}}],"plain":true}""")!.AsObject() };
        var name = _input.Content["profile"]!["name"]!.DeepClone();
        var secret = _input.Content["items"]![0]!["secret"]!.DeepClone();
        _input = _input with { ProtectedValues = [new("profile.name", ProtectionKind.Pii, "subject", name), new("items[0].secret", ProtectionKind.Pii, "subject", secret)] };
        _uses = _input.ProtectedValues.Select(value => new ProtectedValueUse(value.Path, value.Kind, value.Plaintext, new(value.Kind, value.Identifier, _key, 1))).ToArray();
        _original = Compute(_input, _uses);
        _input = _input with { Content = JsonNode.Parse("""{"plain":true,"items":[{"secret":{"a":1,"b":2}}],"profile":{"name":"Alice"}}""")!.AsObject(), ProtectedValues = _input.ProtectedValues.Reverse().ToArray() };
        _uses = _uses.Reverse().ToArray();
    }
    void Because() => _result = Compute(_input, _uses);
    [Fact] void should_sort_protected_paths_and_values_canonically() => _result.Format().ShouldEqual(_original.Format());
    [Fact] void should_pin_dotted_and_indexed_mask_paths() => CanonicalJson.Encode(FingerprintContent.Mask(_input)).ShouldEqual("""{"items":[{"secret":{"$protected":"pii"}}],"plain":true,"profile":{"name":{"$protected":"pii"}}}""");
}
