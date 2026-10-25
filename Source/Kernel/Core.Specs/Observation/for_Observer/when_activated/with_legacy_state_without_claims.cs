// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class with_legacy_state_without_claims : given.an_unsubscribed_observer_with_metrics
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            CatchingUpPartitions = new HashSet<Key> { "partition" },
            PartitionClaims = null,
            ObserverWideClaim = null,
            OwnershipRevision = null
        };

        // Routing clears the partition sets once its lookup succeeds; a failed lookup keeps activation from reaching that cleanup.
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
    }

    async Task Because()
    {
        await Crash();
        await _observer.RegisterCatchingUpPartitions([]);
    }

    [Fact] void should_not_measure_a_legacy_migration_as_divergence() => _metrics.For("chronicle-observer-claim-divergences").ShouldBeEmpty();
    [Fact] void should_seed_a_claim_on_the_first_write() => _stateStorage.State.PartitionClaims.Single().Partition.ShouldEqual((Key)"partition");
    [Fact] void should_seed_the_catch_up_kind() => _stateStorage.State.PartitionClaims.Single().Holder.ShouldEqual(ClaimHolderKind.PartitionCatchUp);
    [Fact] void should_persist_the_seed_revision() => _stateStorage.State.OwnershipRevision.ShouldEqual(new OwnershipRevision(1));
}
