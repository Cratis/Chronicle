// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_an_old_activation_writes : given.a_live_observer_with_guarded_state
{
    ObserverState _before;
    ObserverState _after;

    async Task Because()
    {
        await MoveDefinition();
        _before = await _guarded.Get(_observerId);
        await _observer.SetHandledStats(99);
        await _observer.SetHandledStats(100);
        _after = await _guarded.Get(_observerId);
    }

    [Fact] void should_leave_the_new_owners_state_untouched() => _after.ShouldEqual(_before);
    [Fact] void should_keep_the_new_owner() => _after.Owner.ShouldEqual((EventSequenceId)"another-sequence");
    [Fact] async Task should_be_unsubscribed() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] void should_refresh_the_cached_definition() => _definitionStorage.State.EventSequenceId.ShouldEqual((EventSequenceId)"another-sequence");
}
