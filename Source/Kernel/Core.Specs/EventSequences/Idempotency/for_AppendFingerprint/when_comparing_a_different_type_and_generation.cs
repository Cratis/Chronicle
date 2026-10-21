// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_a_different_type_and_generation : given.an_append_intent
{
    FingerprintComparisonResult _comparison;
    void Establish() => _input = _input with { EventType = new("other", 2) };
    async Task Because() => (await AppendFingerprintComparison.Compare(_original, _input, Substitute.For<IEncryptionKeyStorage>(), Substitute.For<IEvictEncryptionKeyCache>(), "store", "ns")).TryGetResult(out _comparison).ShouldBeTrue();
    [Fact] void should_classify_the_type_change_as_content() => _comparison.Reason.ShouldEqual(FingerprintConflictReason.Content);
}
