// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_reactivated_with_an_outstanding_drop : given.a_removal
{
    void Establish()
    {
        _state = _state.BeginAttempt(_clock.GetUtcNow());
        for (var stage = NamespaceRemovalStage.CloseMarker; stage < NamespaceRemovalStage.Drop; stage++)
        {
            _state = _state.CompleteStage(_clock.GetUtcNow()).IssueStage();
        }
    }

    Task Because() => CreateRecovery().Tick(_name);

    [Fact] void should_keep_the_consumed_attempt() => _state.TotalAttempts.ShouldEqual(1);
    [Fact] void should_require_recovery_instead_of_reissuing_unknown_work() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_not_issue_effects() => _effects.Count.ShouldEqual(0);
}
