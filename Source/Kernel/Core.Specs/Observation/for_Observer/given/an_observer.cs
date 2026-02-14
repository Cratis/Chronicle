// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.Streams;
using Orleans.TestKit;
using Orleans.TestKit.Storage;
using IEventSequence = Cratis.Chronicle.Grains.EventSequences.IEventSequence;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer : Specification
{
    protected Observer _observer;
    protected IStreamProvider _streamProvider;
    protected IStreamProvider _sequenceStreamProvider;
    protected IObserverSubscriber _subscriber;
    protected IJobsManager _jobsManager;
    protected IObserverServiceClient _observerServiceClient;
    protected FailedPartitions _failedPartitionsState;
    protected ObserverId _observerId => "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    protected ObserverKey _observerKey => new(_observerId, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
    protected TestKitSilo _silo = new();
    protected IStorage<ObserverState> _stateStorage;
    protected TestStorageStats _storageStats => _silo.StorageStats<Observer, ObserverState>()!;
    protected IStorage<ObserverDefinition> _definitionStorage;
    protected IStorage<FailedPartitions> _failedPartitionsStorage;
    protected TestStorageStats _failedPartitionsStorageStats => _silo.StorageManager.GetStorageStats(nameof(FailedPartition))!;
    protected IEventSequence _eventSequence;
    protected IConfigurationForObserverProvider _configurationProvider;
    protected Observers _observersConfig;

    async Task Establish()
    {
        _observersConfig = new();
        _configurationProvider = Substitute.For<IConfigurationForObserverProvider>();
        _configurationProvider.GetFor(Arg.Any<string>()).Returns(_observersConfig);
        _silo.AddService(_configurationProvider);
        _subscriber = Substitute.For<IObserverSubscriber>();
        _jobsManager = Substitute.For<IJobsManager>();
        _eventSequence = Substitute.For<IEventSequence>();

        _silo.AddProbe(_ => _subscriber);
        _silo.AddProbe(_ => _jobsManager);
        _silo.AddProbe(_ => _eventSequence);

        _failedPartitionsState = Substitute.For<FailedPartitions>();

        _observerServiceClient = Substitute.For<IObserverServiceClient>();
        _silo.AddService(_observerServiceClient);

        var logger = _silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

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

        _eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);

        _observer = await _silo.CreateGrainAsync<Observer>(_observerKey);

        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }

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
        .GetNextSequenceNumberGreaterOrEqualTo(sequenceNumber, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns(sequenceNumber.Next());

    protected void EventSequenceDoesNotHaveNextEvent(EventSequenceNumber sequenceNumber) => _eventSequence
        .GetNextSequenceNumberGreaterOrEqualTo(sequenceNumber, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
        .Returns(sequenceNumber);
}
