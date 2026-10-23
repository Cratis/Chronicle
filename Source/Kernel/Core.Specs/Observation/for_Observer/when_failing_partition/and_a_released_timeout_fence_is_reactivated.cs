// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_released_timeout_fence_is_reactivated : given.an_observer_with_a_superseded_client
{
    IEnumerable<DeliveryFence> _fences;

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["timeout"], string.Empty, FailureKind.Timeout);
        var fence = (await _observer.GetDeliveryFences()).Single();
        await _observer.ReleaseDeliveryFence(fence.Partition, fence.ReleaseBy);
        await Crash();
        _fences = await _observer.GetDeliveryFences();
    }

    [Fact] void should_not_restore_the_released_fence() => _fences.ShouldBeEmpty();
    [Fact] void should_persist_an_authoritative_empty_snapshot() => _stateStorage.State.DeliveryFences.ShouldBeEmpty();
    [Fact] void should_still_have_the_old_timeout_attempt() => _failedPartitionsStorage.State.Partitions.Single().LastAttempt.Kind.ShouldEqual(FailureKind.Timeout);
}
