// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceRemovalRecovery;

public class when_the_attempt_deadline_expires_between_stages : given.a_removal
{
    void Establish() => _execute = _ =>
    {
        _clock.Advance(TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    };

    Task Because() => _recovery.Tick(_name);

    [Fact] void should_not_start_an_effect_without_remaining_budget() => _effects.ShouldEqual([NamespaceRemovalStage.CloseMarker]);
    [Fact] void should_stop_with_a_terminal_recovery_state() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] void should_not_record_a_destructive_effect_as_issued() => _state.DestructiveAttemptOutstanding.ShouldBeFalse();
}
