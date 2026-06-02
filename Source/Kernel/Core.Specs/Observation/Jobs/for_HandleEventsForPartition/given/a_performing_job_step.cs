// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_performing_job_step : Specification
{
    protected TestableHandleEventsForPartition _jobStep;
    protected IPersistentState<HandleEventsForPartitionState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected Storage.IStorage _storage;
    protected IObserver _observer;
    protected ISomeObserverType _observerSubscriber;
    protected IEventCursor _eventCursor;
    protected HandleEventsForPartitionState _performState;
    protected static readonly EventSequenceNumber first_event_sequence_number = 42ul;

    async Task Establish()
    {
        var eventStoreStorage = Substitute.For<Storage.IEventStoreStorage>();
        var namespaceStorage = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        var eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var eventTypesStorage = Substitute.For<IEventTypesStorage>();

        _storage = Substitute.For<Storage.IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(eventSequenceStorage);
        eventStoreStorage.EventTypes.Returns(eventTypesStorage);
        eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));

        _eventCursor = Substitute.For<IEventCursor>();
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventSequenceNumber(first_event_sequence_number)]);
        var moveNextCount = 0;
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(moveNextCount++ == 0));

        eventSequenceStorage.GetRange(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_eventCursor));

        var subscription = new ObserverSubscription(
            "observer-id",
            new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            [],
            typeof(ISomeObserverType),
            SiloAddress.Zero);
        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(subscription);
        _observerSubscriber = Substitute.For<ISomeObserverType>();

        _silo.AddProbe(_ => _observer);
        _silo.AddProbe(_ => _observerSubscriber);
        _silo.AddService(_storage);
        _silo.AddService(Substitute.For<IJobStepThrottle>());

        var eventComplianceHelper = Substitute.For<IEventComplianceHelper>();
        eventComplianceHelper
            .DecryptEvents(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        _silo.AddService(eventComplianceHelper);

        var logger = _silo.AddService(NullLogger<HandleEventsForPartition>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _stateStorage = _silo.AddPersistentStateStorage<HandleEventsForPartitionState>(
            nameof(JobStepState),
            WellKnownGrainStorageProviders.JobSteps);

        _performState = new HandleEventsForPartitionState
        {
            ObserverKey = new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log),
            Partition = "some-partition",
            StartEventSequenceNumber = EventSequenceNumber.First,
            EndEventSequenceNumber = EventSequenceNumber.Max,
            EventObservationState = EventObservationState.Initial,
            LastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable
        };

        _jobStep = await _silo.CreateGrainAsync<TestableHandleEventsForPartition>(
            JobStepId.New(),
            new JobStepKey(JobId.New(), "event-store", "event-store-namespace"));
        _jobStep.SetupForTesting(_observer, _observerSubscriber, "some-partition");
    }
}
