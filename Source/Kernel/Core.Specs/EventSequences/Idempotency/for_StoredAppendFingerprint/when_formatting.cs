// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_StoredAppendFingerprint;

/// <summary>
/// Persistent v1 format: these literal golden strings must survive implementation changes.
/// </summary>
public class when_formatting : Specification
{
    string _plain;
    string _protected;
    void Because()
    {
        _plain = new StoredAppendFingerprint(new string('a', 64), new EventType("event", 1), []).Format();
        _protected = new StoredAppendFingerprint(new string('a', 64), new EventType("event", 1), [new(ProtectionKind.EncryptedGlobal, new string('c', 64), 2, new string('d', 64)),
             new(ProtectionKind.Pii, new string('b', 64), 1, new string('e', 64))]).Format();
    }
    [Fact] void should_match_the_golden_string() => _plain.ShouldEqual("v1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;type=5:event|1:1|");
    [Fact] void should_match_the_protected_golden_string() => _protected.ShouldEqual("v1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa;type=5:event|1:1|;pii:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb@1=eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee;encrypted-global:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc@2=dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd");
}
