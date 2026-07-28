// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.given;

public class a_catching_up_in_flight_state : Specification
{
    protected IObserver _observer;
    protected CatchingUpInFlight _state;
    protected ObserverState _storedState;
    protected ObserverState _resultingStoredState;
    protected ObserverKey _observerKey;
    protected IPersistentState<ObserverDefinition> _definitionState;
    protected IPersistentState<FailedPartitions> _failuresState;
    protected IJobsManager _jobsManager;
    protected FailedPartitions _failedPartitions;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observerKey = new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), EventSequenceId.Log);

        _definitionState = Substitute.For<IPersistentState<ObserverDefinition>>();
        _definitionState.State = ObserverDefinition.Empty;

        _failedPartitions = new FailedPartitions();
        _failuresState = Substitute.For<IPersistentState<FailedPartitions>>();
        _failuresState.State = _failedPartitions;

        _jobsManager = Substitute.For<IJobsManager>();

        _state = new CatchingUpInFlight(
            _observerKey,
            _definitionState,
            _failuresState,
            _jobsManager,
            Substitute.For<ILogger<CatchingUpInFlight>>());
        _state.SetStateMachine(_observer);

        _storedState = new ObserverState
        {
            Identifier = _observerKey.ObserverId,
            RunningState = ObserverRunningState.Unknown,
        };
    }
}
