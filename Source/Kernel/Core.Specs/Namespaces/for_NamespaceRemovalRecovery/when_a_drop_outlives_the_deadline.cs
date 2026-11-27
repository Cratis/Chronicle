// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_a_drop_outlives_the_deadline : given.a_removal
{
    readonly TaskCompletionSource _drop = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish() => _execute = state =>
    {
        if (state.Stage != NamespaceRemovalStage.Drop) return Task.CompletedTask;
        _entered.TrySetResult();
        return _drop.Task;
    };

    async Task Because()
    {
        var running = _recovery.Tick(_name);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await _clock.WaitForPendingTimer();
        await _recovery.Tick(_name);
        _clock.Advance(TimeSpan.FromSeconds(30));
        await running.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await CreateRecovery().Tick(_name);
        _drop.SetResult();
        await CreateRecovery().Tick(_name);
    }

    [Fact] void should_preserve_the_outstanding_drop_intent() => _state.DestructiveAttemptOutstanding.ShouldBeTrue();
    [Fact] void should_require_reconciliation_even_after_a_late_reply() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_never_overlap_or_repeat_the_drop() => _effects.Count(_ => _ == NamespaceRemovalStage.Drop).ShouldEqual(1);
    [Fact] void should_not_advance_on_a_late_reply() => _state.Stage.ShouldEqual(NamespaceRemovalStage.Drop);
}
