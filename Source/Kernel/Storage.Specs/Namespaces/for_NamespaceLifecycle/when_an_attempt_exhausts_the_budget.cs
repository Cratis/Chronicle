// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceLifecycle;

public class when_an_attempt_exhausts_the_budget : Specification
{
    NamespaceLifecycle _before;
    NamespaceLifecycle _after;

    void Establish() => _before = NamespaceLifecycle.Open("orders", NamespaceIncarnationId.New(), DateTimeOffset.UnixEpoch)
        .BeginRemoval(Guid.NewGuid(), DateTimeOffset.UnixEpoch) with
    {
        Stage = NamespaceRemovalStage.Cleanup,
        TotalAttempts = NamespaceLifecycle.MaximumAttempts,
        AttemptInProgress = true
    };

    void Because() => _after = _before.FailAttempt("NamespaceCleanupFailed", DateTimeOffset.UnixEpoch.AddSeconds(1));

    [Fact] void should_require_operator_recovery() => _after.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_advance_exactly_one_compare_and_swap_revision() => _after.Revision.ShouldEqual(_before.Revision + 1);
    [Fact] void should_preserve_the_consumed_budget() => _after.TotalAttempts.ShouldEqual(_before.TotalAttempts);
    [Fact] void should_record_the_stable_diagnostic_code() => _after.LastFailure.ShouldEqual("NamespaceCleanupFailed");
}
