// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_marker_close_repeatedly_fails : given.a_removal
{
    void Establish() => _execute = _ => Task.FromException(new InvalidOperationException("close failed"));

    async Task Because()
    {
        for (var attempt = 0; attempt < NamespaceLifecycle.MaximumAttempts + 1; attempt++)
        {
            await CreateRecovery().Tick(_name);
            _clock.Advance(TimeSpan.FromMinutes(1));
        }
    }

    [Fact] void should_exhaust_the_persisted_budget_across_reactivations() => _state.TotalAttempts.ShouldEqual(NamespaceLifecycle.MaximumAttempts);
    [Fact] void should_stop_automatic_recovery() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_never_forget_or_drop() => _effects.TrueForAll(_ => _ == NamespaceRemovalStage.CloseMarker).ShouldBeTrue();
    [Fact] void should_not_refund_attempts() => _effects.Count.ShouldEqual(NamespaceLifecycle.MaximumAttempts);
}
