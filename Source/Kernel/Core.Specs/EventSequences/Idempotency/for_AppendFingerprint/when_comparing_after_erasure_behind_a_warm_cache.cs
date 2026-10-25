// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_comparing_after_erasure_behind_a_warm_cache : given.a_stored_protected_intent
{
    async Task Establish()
    {
        (await Compare()).Status.ShouldEqual(FingerprintComparisonStatus.Verified);
        await _backing.RecordErasureFor("store", "ns", "subject");
        await _backing.DeleteFor("store", "ns", "subject");
        ChangeProtectedValue();
    }

    async Task Because() => _comparison = await Compare();

    [Fact] void should_classify_as_erased_instead_of_hashing_with_the_cached_key() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);
}
