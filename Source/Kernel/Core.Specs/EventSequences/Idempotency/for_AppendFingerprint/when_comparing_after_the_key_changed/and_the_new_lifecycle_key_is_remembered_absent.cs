// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_new_lifecycle_key_is_remembered_absent : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await NewLifecycle(rememberAbsent: true);
        (await _cache.TryGetFor("store", "ns", "subject", 2)).ShouldBeNull();
    }
    async Task Because() => _comparison = await Compare();
    [Fact] void should_evict_the_negative_entry_then_verify() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Verified);
}
