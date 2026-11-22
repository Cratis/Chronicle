// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_reactivating_with_an_outstanding_drop : given.an_owner
{
    async Task Establish()
    {
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow())
            .BeginRemoval(Guid.NewGuid(), _clock.GetUtcNow()).BeginAttempt(_clock.GetUtcNow());
        for (var stage = NamespaceRemovalStage.CloseMarker; stage < NamespaceRemovalStage.Drop; stage++)
        {
            _state = _state.CompleteStage(_clock.GetUtcNow()).IssueStage();
        }
        await _owner.OnActivateAsync(default);
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_stop_in_terminal_recovery() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] async Task should_not_issue_another_drop() => await _steps.DidNotReceive().Execute(_eventStore, _name, NamespaceRemovalStage.Drop);
    [Fact] void should_clear_the_timer_after_persisting_terminal_state() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
