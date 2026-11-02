// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.given;

/// <summary>
/// An observer whose claim was granted to a newer job under <see cref="CurrentEpoch"/>, while a job started under
/// <see cref="StaleEpoch"/> may still report back.
/// </summary>
public class an_observer_holding_a_newer_claim : for_Observer.given.an_observer
{
    protected const ulong StaleEpoch = 1;
    protected const ulong CurrentEpoch = 2;
    protected static readonly Key _partition = "partition";

    protected async Task HoldPartition(ClaimHolderKind holder)
    {
        _stateStorage.State = _stateStorage.State with
        {
            ReplayingPartitions = holder == ClaimHolderKind.PartitionReplay ? new HashSet<Key> { _partition } : new HashSet<Key>(),
            CatchingUpPartitions = holder == ClaimHolderKind.PartitionCatchUp ? new HashSet<Key> { _partition } : new HashSet<Key>(),
            PartitionClaims = [new(_partition, holder, null, new(CurrentEpoch), 43UL, [])],
            OwnershipRevision = new(CurrentEpoch)
        };

        // The newer job is outstanding, so reactivating keeps the partition's hold and claim, as in a running silo.
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = holder == ClaimHolderKind.PartitionReplay
                ? new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 43UL, EventSequenceNumber.Max, []) { OwnershipEpoch = CurrentEpoch }
                : new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 43UL, []) { OwnershipEpoch = CurrentEpoch }
        });
        await Crash();
    }

    protected async Task HoldObserverReplay()
    {
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 45UL,
            IsReplaying = true,
            ObserverWideClaim = new(ClaimHolderKind.ObserverReplay, null, new(CurrentEpoch), EventSequenceNumber.First, []),
            OwnershipRevision = new(CurrentEpoch)
        };
        await Crash();
    }

    protected PartitionClaim? ClaimOnPartition => (_stateStorage.State.PartitionClaims ?? []).FirstOrDefault(_ => _.Partition == _partition);

    protected bool AdmitsNewerPartitionCatchUp() =>
        !_observer.IsObsolete(new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 43UL, []) { OwnershipEpoch = CurrentEpoch });

    protected bool AdmitsNewerPartitionReplay() =>
        !_observer.IsObsolete(new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 43UL, EventSequenceNumber.Max, []) { OwnershipEpoch = CurrentEpoch });

    protected bool AdmitsNewerObserverReplay() =>
        !_observer.IsObsolete(new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []) { OwnershipEpoch = CurrentEpoch });
}
