// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_persisted_timeout_fence_has_expired : given.an_observer_with_a_superseded_client
{
    IEnumerable<DeliveryFence> _fences;

    async Task Because()
    {
        _stateStorage.State = _stateStorage.State with
        {
            DeliveryFences = [new("partition", DateTimeOffset.UnixEpoch, [])]
        };
        await Crash();
        _fences = await _observer.GetDeliveryFences();
    }

    [Fact] void should_not_restore_the_expired_fence() => _fences.ShouldBeEmpty();
    [Fact] void should_prune_the_state_snapshot() => _stateStorage.State.DeliveryFences.ShouldBeEmpty();
}
