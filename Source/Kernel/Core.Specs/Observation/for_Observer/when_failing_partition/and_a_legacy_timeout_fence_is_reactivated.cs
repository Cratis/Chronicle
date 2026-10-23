// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_legacy_timeout_fence_is_reactivated : given.an_observer_with_a_superseded_client
{
    IEnumerable<DeliveryFence> _fences;

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout);
        _stateStorage.State = _stateStorage.State with { DeliveryFences = null };
        await Crash();
        _fences = await _observer.GetDeliveryFences();
    }

    [Fact] void should_restore_the_hold_from_the_legacy_failure() => _fences.Single().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_hold_conservatively_without_targets() => _fences.Single().Targets.ShouldBeEmpty();
}
