// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayAdmission.given;

/// <summary>
/// A real replay job on a silo that asks the observer job admission before it runs. The observer's persisted state is
/// what the admission reads, so a spec moves the claim by changing it, and holds the read with a gate to land a start
/// across a change.
/// </summary>
public class an_admitting_replay_job : for_ReplayObserver.given.a_replay_observer_job
{
    protected ObserverState _persistedObserverState;
    protected TaskCompletionSource<ObserverState>? _observerStateRead;

    void Establish()
    {
        _persistedObserverState = ObserverState.Empty;
        _silo.AddService<IEnumerable<IJobAdmission>>([new ObserverJobAdmission(_storage, [new ObserverOwnershipEpochRule()])]);
        _namespaceStorage.Observers.Get(Arg.Any<ObserverId>())
            .Returns(_ => _observerStateRead?.Task ?? Task.FromResult(_persistedObserverState));
    }

    protected static ObserverState ClaimedByReplay(ulong epoch) =>
        ObserverState.Empty with { ObserverWideClaim = new(ClaimHolderKind.ObserverReplay, null, new(epoch), EventSequenceNumber.First, []) };

    protected ReplayObserverRequest RequestWithEpoch(ulong epoch) => _request with { OwnershipEpoch = epoch };

    protected void HoldTheObserverStateRead() =>
        _observerStateRead = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected void PersistStoppedJob(ReplayObserverRequest request)
    {
        _stateStorage.State.Request = request;
        _stateStorage.State.Status = JobStatus.Stopped;
        _stateStorage.State.StatusChanges =
        [
            new() { Status = JobStatus.StartingSteps, Occurred = DateTimeOffset.UtcNow },
            new() { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow },
            new() { Status = JobStatus.Stopped, Occurred = DateTimeOffset.UtcNow }
        ];
    }
}
