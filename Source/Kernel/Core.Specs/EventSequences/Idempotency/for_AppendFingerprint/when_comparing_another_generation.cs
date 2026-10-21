// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_another_generation : given.an_append_intent
{
    FingerprintComparisonResult _comparison;
    void Establish() => _input = _input with { EventType = _input.EventType with { Generation = 2 } };
    async Task Because() => (await AppendFingerprintComparison.Compare(_original, _input, Substitute.For<IEncryptionKeyStorage>(), Substitute.For<IEvictEncryptionKeyCache>(), "store", "ns")).TryGetResult(out _comparison).ShouldBeTrue();
    [Fact] void should_name_the_generation_difference() => _comparison.Reason.ShouldEqual(FingerprintConflictReason.Generation);
    [Fact] void should_retain_the_original_generation() => _comparison.StoredEventType.Generation.Value.ShouldEqual(1u);
}
