// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_key_was_erased_while_missing : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await _cache.DeleteFor("store", "ns", "subject");
        await _cache.RecordErasureFor("store", "ns", "subject");
        (await _cache.GetErasureFor("store", "ns", "subject"))!.ErasedKeyFingerprints.ShouldBeEmpty();
    }
    async Task Because() => _comparison = await Compare();
    [Fact] void should_recognize_the_erasure_floor() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);
}
