// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_key_was_deleted_without_an_erasure : given.a_stored_protected_intent
{
    async Task Establish() => await _cache.DeleteFor("store", "ns", "subject");
    async Task Because() => _comparison = await Compare();
    [Fact] void should_report_the_unverifiable_kind() => _comparison.UnverifiableKind.ShouldEqual(ProtectionKind.Pii);
    [Fact] void should_refuse_the_unverifiable_part() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Unverifiable);
}
