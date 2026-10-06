// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation.Alerts;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Metrics;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Cratis.Traces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute.Extensions;
using Orleans.Core;
using Orleans.Streams;
using Orleans.TestKit;
using Orleans.TestKit.Storage;
using AppendedEventsQueueSubscription = Cratis.Chronicle.EventSequences.AppendedEventsQueueSubscription;
using IAppendedEventsQueues = Cratis.Chronicle.EventSequences.IAppendedEventsQueues;
using IChronicleEventStoreStorage = Cratis.Chronicle.Storage.IEventStoreStorage;
using IChronicleStorage = Cratis.Chronicle.Storage.IStorage;
using IEventSequence = Cratis.Chronicle.EventSequences.IEventSequence;
using IEventStoreNamespaceStorage = Cratis.Chronicle.Storage.IEventStoreNamespaceStorage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer : Specification
{
    protected Observer _observer;
    protected IStreamProvider _streamProvider;
    protected IStreamProvider _sequenceStreamProvider;
    protected IObserverSubscriber _subscriber;
    protected IJobsManager _jobsManager;
    protected IObserverAlerts _observerAlerts;
    protected IObserverServiceClient _observerServiceClient;
    protected FailedPartitions _failedPartitionsState;
    protected virtual ObserverId _observerId => "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    protected ObserverKey _observerKey => new(_observerId, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
    protected TestKitSilo _silo = new();
    protected IStorage<ObserverState> _stateStorage;
    protected TestStorageStats _storageStats => _silo.StorageStats<Observer, ObserverState>()!;
    protected IStorage<ObserverDefinition> _definitionStorage;
    protected IStorage<FailedPartitions> _failedPartitionsStorage;
    protected TestStorageStats _failedPartitionsStorageStats => _silo.StorageManager.GetStorageStats(nameof(FailedPartition))!;
    protected IEventSequence _eventSequence;
    protected IAppendedEventsQueues _appendedEventsQueues;
    protected IConfigurationForObserverProvider _configurationProvider;
    protected IChronicleStorage _storage;
    protected IChronicleEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    protected IJobStorage _jobStorage;
    protected IInFlightEventsStorage _inFlightEventsStorage;
    protected IObserverHandledCountsStorage _observerHandledCountsStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected IEventSequenceStorage _eventSequenceStorage;
    protected IEventCompliance _eventCompliance;
    protected Observers _observersConfig;
    protected NullLogger<Observer> _logger;
    protected ILoggerFactory _loggerFactory;
    protected IMeter<Observer>? _meter;

    protected virtual Observers CreateObserversConfig() => new();

    void AddServices(TestKitSilo silo)
    {
        silo.AddService(_configurationProvider);
        silo.AddService(_storage);
        silo.AddService(_eventCompliance);
        silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        silo.AddService(_observerServiceClient);
        silo.AddKeyedService<IActivitySource<Observer>>(WellKnown.MeterName, new ActivitySource<Observer>());
        silo.AddService(_logger);
        silo.AddService(_loggerFactory);

        if (_meter is not null)
        {
            silo.AddKeyedService(WellKnown.MeterName, _meter);
        }

        silo.AddProbe(_ => _observerAlerts);
        silo.AddProbe(_ => _subscriber);
        silo.AddProbe(_ => _jobsManager);
        silo.AddProbe(_ => _appendedEventsQueues);
        silo.AddProbe(_ => _eventSequence);
    }

    /// <summary>
    /// Creates the meter the observer records its metrics on. By default there is none, so the observer records nothing.
    /// </summary>
    /// <returns>The <see cref="IMeter{T}"/> to register, or null for none.</returns>
    protected virtual IMeter<Observer>? CreateMeter() => null;

    async Task Establish()
    {
        _observersConfig = CreateObserversConfig();
        _configurationProvider = Substitute.For<IConfigurationForObserverProvider>();
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_observersConfig);
        _subscriber = Substitute.For<IObserverSubscriber>();
        _jobsManager = Substitute.For<IJobsManager>();
        _observerAlerts = Substitute.For<IObserverAlerts>();
        ApplyAlertReports();
        _eventSequence = Substitute.For<IEventSequence>();

        // A subscribed observer is on its queue - the queue only drops it behind the observer's back when it spills
        // to catch-up under back-pressure, which the specs that care about set up explicitly.
        _appendedEventsQueues = Substitute.For<IAppendedEventsQueues>();
        _appendedEventsQueues
            .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>())
            .Returns(callInfo => new AppendedEventsQueueSubscription(callInfo.Arg<ObserverKey>(), 0));
        _appendedEventsQueues.IsSubscribed(Arg.Any<ObserverKey>()).Returns(true);

        _storage = Substitute.For<IChronicleStorage>();
        _eventStoreStorage = Substitute.For<IChronicleEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _inFlightEventsStorage = Substitute.For<IInFlightEventsStorage>();
        _observerHandledCountsStorage = Substitute.For<IObserverHandledCountsStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _eventSequenceStorage = Substitute.For<IEventSequenceStorage>();
        _eventCompliance = Substitute.For<IEventCompliance>();

        // Wire the storage chain: IStorage → IEventStoreStorage → IEventTypesStorage and IEventStoreNamespaceStorage → IInFlightEventsStorage / IObserverHandledCountsStorage
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_eventStoreNamespaceStorage);
        _eventStoreNamespaceStorage.InFlightEvents.Returns(_inFlightEventsStorage);
        _inFlightEventsStorage.GetFor(Arg.Any<ObserverId>()).Returns([]);
        _eventStoreNamespaceStorage.ObserverHandledCounts.Returns(_observerHandledCountsStorage);
        _eventStoreNamespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequenceStorage);

        // By default, every job looked up individually is still running - so nothing it concluded is forgotten.
        _jobStorage = Substitute.For<IJobStorage>();
        _eventStoreNamespaceStorage.Jobs.Returns(_jobStorage);
        _jobStorage.GetJob(Arg.Any<JobId>()).Returns(call => Task.FromResult<Catch<JobState, Cratis.Orleans.Storage.Jobs.JobError>>(new JobState
        {
            Id = call.Arg<JobId>(),
            Status = JobStatus.Running
        }));
        _observerHandledCountsStorage.GetFor(Arg.Any<ObserverId>(), Arg.Any<Key>()).Returns(new Dictionary<EventTypeId, EventCount>());

        // By default, no schemas are known — events pass through unchanged.
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns([]);

        // By default, the compliance helper passes events through unchanged.
        _eventCompliance
            .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray()));

        _failedPartitionsState = Substitute.For<FailedPartitions>();

        _observerServiceClient = Substitute.For<IObserverServiceClient>();

        _logger = NullLogger<Observer>.Instance;
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);

        _meter = CreateMeter();
        AddServices(_silo);

        _stateStorage = _silo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName);
        _definitionStorage = _silo.StorageManager.GetStorage<ObserverDefinition>(nameof(ObserverDefinition));
        _definitionStorage.State = new ObserverDefinition
        {
            Identifier = _observerId,
            IsReplayable = true,
        };
        await _definitionStorage.WriteStateAsync();

        _failedPartitionsStorage = _silo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        _failedPartitionsStorage.State = _failedPartitionsState;
        _eventStoreNamespaceStorage.Observers.Get(_observerId).Returns(_ => _stateStorage.State with { Identifier = _observerId });
        _eventStoreNamespaceStorage.FailedPartitions.GetFor(_observerId).Returns(_ => _failedPartitionsStorage.State);

        _eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>()).Returns(EventSequenceNumber.Unavailable);

        _jobsManager.GetJobs(Arg.Any<JobQuery>())
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));

        _observer = await _silo.CreateGrainAsync<Observer>(_observerKey);
        await _observer.ReportAlertState();
        _observerAlerts.ClearReceivedCalls();

        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }

    /// <summary>
    /// Simulates the observer grain being deactivated and then activated again in a fresh silo, with the persisted
    /// state, definition and failed partitions carried over from the grain that was deactivated.
    /// </summary>
    /// <returns>The reactivated <see cref="Observer"/>, which also becomes the observer under specification.</returns>
    protected async Task<Observer> Reactivate()
    {
        await _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);

        return await Crash();
    }

    protected void ApplyAlertReports() => _observerAlerts.Configure().Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call =>
    {
        // NSubstitute also invokes an existing return callback with matcher placeholders during reconfiguration.
        return call[0] is ObserverAlertSnapshot snapshot
            ? new ObserverAlertReceipt(snapshot.LifecycleId, snapshot.Revision, ObserverAlertReconciliation.Applied)
            : new ObserverAlertReceipt(Guid.Empty, 0, ObserverAlertReconciliation.RetryRequired);
    });

    protected void FailAlertReports(Exception error) => _observerAlerts.Configure().Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(Task.FromException<ObserverAlertReceipt>(error));

    protected Task<bool> ReportAlerts() => _observer.ReportAlertState();

    /// <summary>
    /// Discards activation memory without running OnDeactivateAsync or its flushes.
    /// </summary>
    /// <param name="resetStorageStatistics">Whether to reset the write counts after activation.</param>
    /// <returns>The fresh activation backed by copies of the stored records.</returns>
    protected async Task<Observer> Crash(bool resetStorageStatistics = true)
    {
        var persistedState = _stateStorage.State with
        {
            ReplayingPartitions = _stateStorage.State.ReplayingPartitions.ToHashSet(),
            CatchingUpPartitions = _stateStorage.State.CatchingUpPartitions.ToHashSet(),
            InFlightPartitions = _stateStorage.State.InFlightPartitions.ToHashSet()
        };
        var persistedDefinition = _definitionStorage.State with { EventTypes = _definitionStorage.State.EventTypes.ToArray() };
        var persistedFailures = new FailedPartitions
        {
            Partitions = _failedPartitionsStorage.State.Partitions.Select(partition => new FailedPartition
            {
                Id = partition.Id,
                ObserverId = partition.ObserverId,
                Partition = partition.Partition,
                IsQuarantined = partition.IsQuarantined,
                IsResolved = partition.IsResolved,
                Attempts = partition.Attempts.ToArray()
            }).ToArray()
        };

        _silo = new();
        AddServices(_silo);
        _stateStorage = _silo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName);
        _stateStorage.State = persistedState;
        _definitionStorage = _silo.StorageManager.GetStorage<ObserverDefinition>(nameof(ObserverDefinition));
        _definitionStorage.State = persistedDefinition;
        _failedPartitionsStorage = _silo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        _failedPartitionsStorage.State = persistedFailures;
        _failedPartitionsState = persistedFailures;

        _observer = await _silo.CreateGrainAsync<Observer>(_observerKey);
        if (resetStorageStatistics)
        {
            _storageStats.ResetCounts();
        }
        return _observer;
    }

    protected void GivenFailedEventAt(Key partition, EventSequenceNumber sequenceNumber, EventType eventType) =>
        _eventSequenceStorage.GetEventAt(sequenceNumber).Returns(
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, sequenceNumber) with
            {
                Context = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(eventType, sequenceNumber).Context with
                {
                    EventSourceId = partition.ToString()
                }
            });

    protected void CheckStartedCatchupJob(EventSequenceNumber lastHandled, Key _partition) => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            Arg.Is<CatchUpObserverPartitionRequest>(_ =>
                _.ObserverKey == _observerKey &&
                _.Key == _partition &&
                _.FromSequenceNumber == lastHandled.Next() &&
                _.EventTypes.SequenceEqual(_definitionStorage.State.EventTypes)));

    protected void CheckDidNotStartCatchupJob() => _jobsManager.DidNotReceive()
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());

    protected void EventSequenceHasNextEvent(EventSequenceNumber sequenceNumber) => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(sequenceNumber.Next(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns(sequenceNumber.Next());

    protected void EventSequenceDoesNotHaveNextEvent(EventSequenceNumber sequenceNumber) => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(sequenceNumber.Next(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns(EventSequenceNumber.Unavailable);
}
