// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_subject_was_erased : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await Erase();
        ChangeProtectedValue();
    }
    async Task Because() => _comparison = await Compare();
    [Fact] void should_verify_only_the_knowable_parts() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.VerifiedAfterErasure);
}
