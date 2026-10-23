// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_only_the_state_stamp_is_stale : given.a_live_observer_with_guarded_state
{
    async Task Because()
    {
        await _guarded.ClaimOwnership(_observerId, "another-sequence");
        await _observer.SetHandledStats(99);
    }

    [Fact] async Task should_reclaim_the_stamp_for_the_stored_definition() =>
        (await _guarded.Get(_observerId)).Owner.ShouldEqual(_observerKey.EventSequenceId);
}
