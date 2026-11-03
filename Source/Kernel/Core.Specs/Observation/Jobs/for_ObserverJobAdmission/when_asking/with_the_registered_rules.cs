// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.when_asking;

public class with_the_registered_rules : given.rules
{
    ObserverJobAdmission _admission;
    bool _result;

    void Establish()
    {
        var key = new ObserverKey("observer", "event-store", "namespace", "event-sequence");
        _request = new CatchUpObserverPartitionRequest(key, ObserverType.Reactor, (Key)"partition", EventSequenceNumber.First, []) { OwnershipEpoch = 1 };
        _storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Observers.Get(key.ObserverId)
            .Returns(new ObserverState { PartitionClaims = [new("partition", ClaimHolderKind.PartitionCatchUp, null, new(2), EventSequenceNumber.First, [])] });
        _admission = new(_storage, [new ObserverOwnershipEpochRule()]);
    }

    async Task Because() => _result = await _admission.MayRun(AdmissionFor(_request), CancellationToken.None);

    [Fact] void should_refuse_a_job_whose_claim_moved_on() => _result.ShouldBeFalse();
}
