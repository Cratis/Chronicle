// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up;

public class and_shadow_claim_is_handed_over : for_Observer.given.an_observer_with_metrics
{
    int _divergencesBeforeCompletion;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            CatchingUpPartitions = new HashSet<Key> { "partition" },
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, null, new(1), 42, [])],
            OwnershipRevision = new(1)
        };
        await Crash();
        _divergencesBeforeCompletion = _metrics.SumOf("chronicle-observer-claim-divergences");
    }

    async Task Because() => await _observer.PartitionCaughtUp("partition", 42, 42);

    [Fact] void should_remove_the_claim() => _stateStorage.State.PartitionClaims.ShouldBeEmpty();
    [Fact] void should_not_measure_a_normal_hand_over_as_divergence() => _metrics.SumOf("chronicle-observer-claim-divergences").ShouldEqual(_divergencesBeforeCompletion);
    [Fact] void should_keep_the_authoritative_completion() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_advance_the_revision_on_hand_over() => _stateStorage.State.OwnershipRevision.ShouldEqual(new OwnershipRevision(2));
}
