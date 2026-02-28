// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States.for_Replay.given;

public class a_replay_state : Specification
{
    protected IObserver _observer;
    protected ObserverKey _observerKey;
    protected IJobsManager _jobsManager;
    protected ObserverSubscription _subscription;
    protected ObserverState _storedState;
    protected ObserverDefinition _observerDefinition;
    protected ObserverState _resultingStoredState;
    protected IPersistentState<ObserverDefinition> _definitionState;
    protected Replay _state;
    protected ObserverId _observerId;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _observerId = Guid.NewGuid().ToString();
        _observerKey = new(_observerId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        _jobsManager = Substitute.For<IJobsManager>();

        _observerDefinition = ObserverDefinition.Empty;
        _definitionState = Substitute.For<IPersistentState<ObserverDefinition>>();
        _definitionState.State.Returns(c => _observerDefinition);

        _state = new Replay(
            _observerKey,
            _definitionState,
            _jobsManager,
            Substitute.For<ILogger<Replay>>());
        _state.SetStateMachine(_observer);

        _storedState = new ObserverState
        {
            Identifier = _observerId,
            RunningState = ObserverRunningState.Unknown,
        };

        _subscription = new ObserverSubscription(
            _storedState.Identifier,
            new(_storedState.Identifier, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log),
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);

        _observer.GetSubscription().Returns(_ => Task.FromResult(_subscription));

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Enumerable.Empty<JobState>().ToImmutableList());
    }
}
