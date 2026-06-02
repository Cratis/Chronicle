// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.StateMachines;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using IChronicleEventStoreStorage = Cratis.Chronicle.Storage.IEventStoreStorage;
using IChronicleStorage = Cratis.Chronicle.Storage.IStorage;
using IEventStoreNamespaceStorage = Cratis.Chronicle.Storage.IEventStoreNamespaceStorage;

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
    protected IChronicleStorage _storage;
    protected IChronicleEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    protected IInFlightEventsStorage _inFlightEventsStorage;
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

        _storage = Substitute.For<IChronicleStorage>();
        _eventStoreStorage = Substitute.For<IChronicleEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _inFlightEventsStorage = Substitute.For<IInFlightEventsStorage>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_eventStoreNamespaceStorage);
        _eventStoreNamespaceStorage.InFlightEvents.Returns(_inFlightEventsStorage);
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>()).Returns([]);

        _jobsManager = Substitute.For<IJobsManager>();

        _state = new CatchingUpInFlight(
            _observerKey,
            _definitionState,
            _failuresState,
            _storage,
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
