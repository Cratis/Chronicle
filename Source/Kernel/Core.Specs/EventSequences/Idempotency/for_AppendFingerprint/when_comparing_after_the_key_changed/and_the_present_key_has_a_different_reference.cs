// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_present_key_has_a_different_reference : given.a_stored_protected_intent
{
    async Task Establish() => await _backing.SaveFor("store", "ns", "subject", new([7], [8]), 1);
    async Task Because() => _comparison = await Compare();
    [Fact] void should_refuse_to_verify_with_different_material() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Unverifiable);
}
