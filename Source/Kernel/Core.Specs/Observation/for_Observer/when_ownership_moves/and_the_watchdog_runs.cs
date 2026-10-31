// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_the_watchdog_runs : given.a_live_observer_with_guarded_state
{
    ObserverState _before;

    async Task Because()
    {
        await MoveDefinition();
        _before = await _guarded.Get(_observerId);
        await _observer.RunWatchdogAsync();
        await _observer.SetHandledStats(99);
    }

    [Fact] async Task should_keep_the_new_state() => (await _guarded.Get(_observerId)).ShouldEqual(_before);
    [Fact] void should_refresh_the_cached_definition() => _definitionStorage.State.EventSequenceId.ShouldEqual((EventSequenceId)"another-sequence");
    [Fact] async Task should_remain_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
}
