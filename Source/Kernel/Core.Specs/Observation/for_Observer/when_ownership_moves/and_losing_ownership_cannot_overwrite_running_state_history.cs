// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_losing_ownership_cannot_overwrite_running_state_history : given.a_live_observer_with_guarded_state
{
    readonly DateTimeOffset _entryTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    ObserverState _claimed;
    ObserverState _after;

    async Task Establish()
    {
        var persisted = await _guarded.Get(_observerId);
        await _guarded.Save(persisted with { RunningStateChangedAt = _entryTime }, _observerKey.EventSequenceId);
    }

    async Task Because()
    {
        await MoveDefinition();
        _claimed = await _guarded.Get(_observerId);
        await _observer.SetHandledStats(99UL);
        await _observer.SetHandledStats(100UL);
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);
        _after = await _guarded.Get(_observerId);
    }

    [Fact] void should_preserve_the_new_owners_entire_record() => _after.ShouldEqual(_claimed);
    [Fact] void should_not_stamp_disconnection_or_deactivation() => _after.RunningStateChangedAt.ShouldEqual(_entryTime);
    [Fact] void should_not_replace_the_new_owners_running_state() => _after.RunningState.ShouldEqual(ObserverRunningState.Active);
}
