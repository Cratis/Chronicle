// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.StateMachines;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.given;

public class a_catching_up_in_flight_state_with_ownership_claims : a_catching_up_in_flight_state
{
    protected readonly Key _partition = "partition";
    protected IObserverOwnershipClaims _claims;
    protected List<CatchUpObserverPartitionRequest> _started;

    void Establish()
    {
        _observer = Substitute.For<IObserver, IStateMachine<ObserverState>, IObserverOwnershipClaims>();
        _stateMachine = (IStateMachine<ObserverState>)_observer;
        _claims = (IObserverOwnershipClaims)_observer;
        _state.SetStateMachine(_stateMachine);
        _claims.GrantPartitionsBeforeStart(Arg.Any<IEnumerable<Key>>(), ClaimHolderKind.PartitionCatchUp, Arg.Any<EventSequenceNumber>())
            .Returns(callInfo => callInfo.Arg<IEnumerable<Key>>().ToDictionary(_ => _, _ => (ulong?)11UL));
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(ImmutableList<JobState>.Empty);
        _started = [];
        _jobsManager
            .When(_ => _.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>()))
            .Do(callInfo => _started.Add(callInfo.Arg<CatchUpObserverPartitionRequest>()));
    }

    protected static JobState PartitionCatchUpJob(CatchUpObserverPartitionRequest request, JobStatus status) => new()
    {
        Id = JobId.New(),
        Request = request,
        StatusChanges = [new() { Status = status, Occurred = DateTimeOffset.UtcNow }],
        Status = status
    };
}
