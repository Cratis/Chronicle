// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_a_new_lifecycle_was_allowed : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await Erase();
        await _cache.AllowNewKeyFor("store", "ns", "subject");
        await _backing.GetOrAddWithRevisionFor("store", "ns", "subject", new([7, 8, 9], [10, 11, 12]));
        ChangeProtectedValue();
    }
    async Task Because() => _comparison = await Compare();
    [Fact] void should_not_compare_the_old_part_using_the_new_key() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);
}
