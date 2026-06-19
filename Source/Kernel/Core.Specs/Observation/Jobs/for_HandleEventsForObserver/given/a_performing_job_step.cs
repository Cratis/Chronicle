// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

public class a_performing_job_step : Specification
{
    protected TestableHandleEventsForObserver _jobStep;
    protected IPersistentState<HandleEventsForObserverState> _stateStorage;
    protected TestKitSilo _silo = new();
    protected Storage.IStorage _storage;
    protected IObserver _observer;
    protected ISomeObserverType _observerSubscriber;
    protected IEventCursor _eventCursor;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected HandleEventsForObserverState _performState;
    protected EventSourceId? _eventSourceIdFilter;
    protected readonly List<HandledBatch> _handledBatches = [];

    protected static AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, EventSourceId eventSourceId) =>
        AppendedEvent.EmptyWithEventSequenceNumber(sequenceNumber) with
        {
            Context = EventContext.Empty with
            {
                SequenceNumber = sequenceNumber,
                EventSourceId = eventSourceId
            }
        };

    async Task Establish()
    {
        var eventStoreStorage = Substitute.For<Storage.IEventStoreStorage>();
        var namespaceStorage = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        var eventTypesStorage = Substitute.For<IEventTypesStorage>();

        _storage = Substitute.For<Storage.IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(namespaceStorage);
        namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);
        eventStoreStorage.EventTypes.Returns(eventTypesStorage);
        eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));

        _eventCursor = Substitute.For<IEventCursor>();
        _eventCursor.Current.Returns([
            CreateEvent(1UL, "module"),
            CreateEvent(2UL, "feature"),
            CreateEvent(3UL, "module")
        ]);
        var moveNextCount = 0;
        _eventCursor.MoveNext().Returns(_ => Task.FromResult(moveNextCount++ == 0));

        _eventSequenceStorage.GetRange(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSequenceNumber>(),
            Arg.Do<EventSourceId?>(value => _eventSourceIdFilter = value),
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_eventCursor));

        var observerKey = new ObserverKey("observer-id", "event-store", "event-store-namespace", EventSequenceId.Log);
        var subscription = new ObserverSubscription(
            "observer-id",
            observerKey,
            [],
            typeof(ISomeObserverType),
            SiloAddress.Zero);
        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(subscription);
        _observerSubscriber = Substitute.For<ISomeObserverType>();
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var partition = call.Arg<Key>();
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                _handledBatches.Add(new(partition, events.Select(_ => _.Context.SequenceNumber).ToArray()));
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });

        _silo.AddProbe(_ => _observer);
        _silo.AddProbe(_ => _observerSubscriber);
        _silo.AddService(_storage);
        _silo.AddService(Substitute.For<IJobStepThrottle>());

        var eventCompliance = Substitute.For<IEventCompliance>();
        eventCompliance
            .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        _silo.AddService(eventCompliance);

        var logger = _silo.AddService(NullLogger<HandleEventsForObserver>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _stateStorage = _silo.AddPersistentStateStorage<HandleEventsForObserverState>(
            nameof(JobStepState),
            WellKnownGrainStorageProviders.JobSteps);

        _performState = new HandleEventsForObserverState
        {
            ObserverKey = observerKey,
            StartEventSequenceNumber = EventSequenceNumber.First,
            EndEventSequenceNumber = EventSequenceNumber.Max,
            EventObservationState = EventObservationState.Replay,
            LastSuccessfullyHandledEventSequenceNumber = EventSequenceNumber.Unavailable
        };

        _jobStep = await _silo.CreateGrainAsync<TestableHandleEventsForObserver>(
            JobStepId.New(),
            new JobStepKey(JobId.New(), "event-store", "event-store-namespace"));
        _jobStep.SetupForTesting(_observer, subscription);
    }
}
