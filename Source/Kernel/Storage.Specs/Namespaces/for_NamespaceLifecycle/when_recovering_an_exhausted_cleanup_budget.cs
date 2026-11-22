// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceLifecycle;

public class when_recovering_an_exhausted_cleanup_budget : Specification
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

    void Because() => _after = _before.RecoverOutstandingAttempt(DateTimeOffset.UnixEpoch.AddSeconds(1))
        .BeginAttempt(DateTimeOffset.UnixEpoch.AddSeconds(2));

    [Fact] void should_require_operator_recovery() => _after.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_not_reset_the_budget() => _after.TotalAttempts.ShouldEqual(NamespaceLifecycle.MaximumAttempts);
    [Fact] void should_not_issue_another_attempt() => _after.AttemptInProgress.ShouldBeFalse();
}
