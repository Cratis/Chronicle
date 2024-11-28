// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.Orleans.StateMachines;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.States.for_CatchUp.given;

public class a_catch_up_state : Specification
{
    protected IObserver _observer;
    protected ObserverKey _observerKey;
    protected IJobsManager _jobsManager;
    protected ObserverSubscription _subscription;
    protected ObserverState _storedState;
    protected ObserverState _resultingStoredState;
    protected CatchUp _state;
    protected ObserverId _observerId;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observerId = Guid.NewGuid().ToString();
        _observerKey = new(_observerId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        _jobsManager = Substitute.For<IJobsManager>();
        _state = new CatchUp(_observerKey, _jobsManager, Substitute.For<ILogger<CatchUp>>());
        _state.SetStateMachine(_observer);

        _storedState = new ObserverState
        {
            Id = _observerId,
            RunningState = ObserverRunningState.CatchingUp,
        };

        _subscription = new ObserverSubscription(
            _storedState.Id,
            new(_storedState.Id, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log),
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        _observer.GetSubscription().Returns(_ => _subscription);
    }
}
