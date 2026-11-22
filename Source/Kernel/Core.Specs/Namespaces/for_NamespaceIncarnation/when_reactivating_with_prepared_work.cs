// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnation;

public class when_reactivating_with_prepared_work : given.an_owner
{
    readonly NamespaceIncarnationId _reserved = NamespaceIncarnationId.New();
    int _activationWrites;

    async Task Establish()
    {
        _state = new(_name, _reserved, _clock.GetUtcNow()) { NotifyOnOpen = true };
        await _owner.OnActivateAsync(default);
        _activationWrites = _writes.Count;
    }

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_not_run_creation_inside_activation() => _activationWrites.ShouldEqual(0);
    [Fact] void should_recover_using_the_reserved_identity() => _state.Incarnation.ShouldEqual(_reserved);
    [Fact] void should_open_on_the_recovery_turn() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_clear_the_timer_only_after_open() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
