// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_an_unprotected_intent : given.an_append_intent
{
    FingerprintComparisonResult _comparison;
    IEncryptionKeyStorage _keys;
    void Establish() => _keys = Substitute.For<IEncryptionKeyStorage>();
    async Task Because() => (await AppendFingerprintComparison.Compare(_original, _input, _keys, Substitute.For<IEvictEncryptionKeyCache>(), "store", "ns")).TryGetResult(out _comparison).ShouldBeTrue();
    [Fact] void should_verify_the_plain_intent() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Verified);
    [Fact] void should_not_touch_key_storage() => _keys.ReceivedCalls().ShouldBeEmpty();
}
