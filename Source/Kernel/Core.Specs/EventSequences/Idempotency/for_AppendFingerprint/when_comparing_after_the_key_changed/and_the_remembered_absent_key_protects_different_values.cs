// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint.when_comparing_after_the_key_changed;

public class and_the_remembered_absent_key_protects_different_values : given.a_stored_protected_intent
{
    async Task Establish()
    {
        await NewLifecycle(rememberAbsent: true);
        ChangeProtectedValue();
    }
    async Task Because() => _comparison = await Compare();
    [Fact] void should_refuse_instead_of_mistaking_the_old_fence_for_erasure() => _comparison.Status.ShouldEqual(FingerprintComparisonStatus.Conflict);
    [Fact] void should_report_a_content_conflict() => _comparison.Reason.ShouldEqual(FingerprintConflictReason.Content);
}
