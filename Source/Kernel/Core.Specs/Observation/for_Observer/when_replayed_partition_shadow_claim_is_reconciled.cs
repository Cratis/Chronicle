// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replayed_partition_shadow_claim_is_reconciled : given.an_observer_with_metrics
{
    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            ReplayingPartitions = new HashSet<Key> { "partition" },
            PartitionClaims = [new("partition", ClaimHolderKind.PartitionReplay, null, new(1), 42, [])],
            OwnershipRevision = new(1)
        };
        await Crash();
    }

    async Task Because() => await _observer.PartitionReplayed("partition", 42);

    [Fact] void should_remove_the_claim_on_the_next_write() => _stateStorage.State.PartitionClaims.ShouldBeEmpty();
    [Fact] void should_leave_the_authoritative_replay_completion_unchanged() => _stateStorage.State.ReplayingPartitions.ShouldBeEmpty();
    [Fact] void should_report_the_expected_unhooked_replay_hand_over() => _metrics.For("chronicle-observer-claim-divergences").Single().Tags["divergence"].ShouldEqual("ReleasedWithoutHandOver");
}
