// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOwnershipEpochRule.given;

public class a_rule : Specification
{
    protected ObserverOwnershipEpochRule _rule;
    protected IStorage _storage;
    protected ObserverKey _observerKey;
    protected Key _partition;
    protected ObserverState _persisted;
    protected bool _result;

    void Establish()
    {
        _rule = new();
        _storage = Substitute.For<IStorage>();
        _observerKey = new("observer", "event-store", "namespace", "event-sequence");
        _partition = "partition";
        _persisted = new ObserverState();
        _storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Observers.Get(_observerKey.ObserverId)
            .Returns(_ => _persisted);
        _storage.ClearReceivedCalls();
    }

    protected CatchUpObserverPartitionRequest PartitionCatchUp(ulong? epoch) =>
        new(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, []) { OwnershipEpoch = epoch };

    protected ReplayObserverRequest ObserverReplay(ulong? epoch) =>
        new(_observerKey, ObserverType.Reactor, []) { OwnershipEpoch = epoch };

    protected void PersistPartitionClaim(ClaimHolderKind holder, ulong epoch) =>
        _persisted = _persisted with { PartitionClaims = [new(_partition, holder, null, new(epoch), EventSequenceNumber.First, [])] };

    protected void PersistObserverWideClaim(ClaimHolderKind holder, ulong epoch) =>
        _persisted = _persisted with { ObserverWideClaim = new(holder, null, new(epoch), EventSequenceNumber.First, []) };

    protected Task<bool> Ask(IObserverJobRequest request) =>
        _rule.MayRun(new(new(JobKey.NotSet, JobId.New(), JobType.NotSet, request), request, _storage), CancellationToken.None);
}
