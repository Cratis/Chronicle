// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_StoredAppendFingerprint.when_parsing_the_stored_form;

public class and_it_round_trips : Specification
{
    StoredAppendFingerprint _original;
    StoredAppendFingerprint _parsed;
    void Establish() => _original = new(new string('a', 64), new EventType("é😀;|:@=", 12), Enum.GetValues<ProtectionKind>().Select(kind => new ProtectedFingerprintPart(kind, new string('b', 64), 2, new string('c', 64))).ToArray());
    void Because() => StoredAppendFingerprint.Parse(_original.Format()).TryGetResult(out _parsed).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_text() => _parsed.Format().ShouldEqual(_original.Format());
    [Fact] void should_read_the_length_prefixed_type_id() => _parsed.EventType.ShouldEqual(_original.EventType);
    [Fact] void should_preserve_all_protected_parts() => _parsed.ProtectedParts.ShouldEqual(_original.ProtectedParts);
}
