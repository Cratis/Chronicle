// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_an_encrypted_value_differs : given.an_append_intent
{
    void Establish()
    {
        Protect(kind: ProtectionKind.EncryptedNamespace);
        _input.Content["name"] = "Bob";
        _input = _input with { ProtectedValues = [new("name", ProtectionKind.EncryptedNamespace, "subject", _input.Content["name"]!.DeepClone())] };
    }
    void Because() => _result = Compute(_input, Use("name", ProtectionKind.EncryptedNamespace, _key));
    [Fact] void should_preserve_the_plain_part() => _result.Plain.ShouldEqual(_original.Plain);
    [Fact] void should_change_the_protected_part() => _result.ProtectedParts[0].Hash.ShouldNotEqual(_original.ProtectedParts[0].Hash);
}
