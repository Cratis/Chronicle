// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_replaying_partition_with_shadow_claims : given.an_observer_with_metrics
{
    void Establish() => _stateStorage.State = _stateStorage.State with
    {
        LastHandledEventSequenceNumber = 41,
        NextEventSequenceNumber = 42
    };

    async Task Because() => await _observer.ReplayPartition("partition");

    [Fact] void should_seed_a_partition_replay_claim() => _stateStorage.State.PartitionClaims.Single().Holder.ShouldEqual(ClaimHolderKind.PartitionReplay);
    [Fact] void should_keep_the_partition_identity() => _stateStorage.State.PartitionClaims.Single().Partition.ShouldEqual((Key)"partition");
    [Fact] void should_resume_after_the_last_handled_event() => _stateStorage.State.PartitionClaims.Single().ResumePosition.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_allocate_the_first_revision() => _stateStorage.State.OwnershipRevision.ShouldEqual(new OwnershipRevision(1));
    [Fact] void should_report_the_expected_unhooked_replay_intent() => _metrics.For("chronicle-observer-claim-divergences").Single().Tags["divergence"].ShouldEqual("DerivedSetWithoutClaim");
}
