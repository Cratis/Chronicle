// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class with_claims_diverging_from_sets : given.an_unsubscribed_observer_with_metrics
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            CatchingUpPartitions = new HashSet<Key> { "partition" },
            PartitionClaims = [],
            OwnershipRevision = new(7)
        };

        // Routing clears the partition sets once its lookup succeeds; a failed lookup keeps activation from reaching that cleanup.
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.StorageError);
    }

    async Task Because() => await Crash();

    [Fact] void should_measure_the_missing_claim() => _metrics.For("chronicle-observer-claim-divergences").Single().Tags["divergence"].ShouldEqual("DerivedSetWithoutClaim");
    [Fact] void should_count_the_disagreement_once() => _metrics.SumOf("chronicle-observer-claim-divergences").ShouldEqual(1);
    [Fact] void should_leave_the_authoritative_set_unchanged() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"partition");
}
