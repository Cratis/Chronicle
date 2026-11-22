// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_a_destructive_effect_fails : given.a_removal
{
    void Establish() => _execute = state => state.Stage == NamespaceRemovalStage.Drop
        ? Task.FromException(new InvalidOperationException("drop outcome unknown"))
        : Task.CompletedTask;

    async Task Because()
    {
        await _recovery.Tick(_name);
        _clock.Advance(TimeSpan.FromMinutes(1));
        await CreateRecovery().Tick(_name);
    }

    [Fact] void should_not_treat_a_driver_exception_as_terminal_evidence() => _state.DestructiveAttemptOutstanding.ShouldBeTrue();
    [Fact] void should_require_explicit_reconciliation() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_not_retry_the_drop() => _effects.Count(_ => _ == NamespaceRemovalStage.Drop).ShouldEqual(1);
    [Fact] void should_not_delete_the_listing() => _effects.Contains(NamespaceRemovalStage.DeleteListing).ShouldBeFalse();
}
