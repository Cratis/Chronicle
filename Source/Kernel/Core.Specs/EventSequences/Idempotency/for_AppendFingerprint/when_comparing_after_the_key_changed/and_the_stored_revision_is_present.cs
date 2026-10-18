// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_stored_revision_is_present : given.a_stored_protected_intent
{
    async Task Establish() => await _backing.SaveFor("store", "ns", "subject", new([7], [8]), 2);
    async Task Because() => _comparison = await Compare();
    [Fact] void should_read_the_stored_revision_instead_of_latest() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Verified);
}
