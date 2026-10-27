// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class after_sets_cleared_externally : given.an_observer_with_metrics
{
    async Task Establish()
    {
        _stateStorage.State.CatchingUpPartitions.Add((Key)"partition");
        await _observer.RegisterCatchingUpPartitions([]);
    }

    async Task Because()
    {
        _stateStorage.State.CatchingUpPartitions.Clear();
        await _observer.RegisterCatchingUpPartitions([]);
    }

    [Fact] void should_drop_the_stale_claim() => _stateStorage.State.PartitionClaims.ShouldBeEmpty();
    [Fact] void should_measure_the_release_without_hand_over() => _metrics.For("chronicle-observer-claim-divergences").Count(measurement => Equals(measurement.Tags["divergence"], "ReleasedWithoutHandOver")).ShouldEqual(1);
    [Fact] void should_leave_the_authoritative_set_empty() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
}
