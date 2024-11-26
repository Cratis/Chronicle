// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.Runtime;
using Orleans.Streams;
using Orleans.TestKit;
using Orleans.TestKit.Storage;
using IEventSequence = Cratis.Chronicle.Grains.EventSequences.IEventSequence;

namespace Cratis.Chronicle.Grains.Observation.for_Observer.given;

public class an_observer : Specification
{
    protected Observer _observer;
    protected IStreamProvider _streamProvider;
    protected IStreamProvider _sequenceStreamProvider;
    protected IObserverSubscriber _subscriber;
    protected IObserverServiceClient _observerServiceClient;
    protected FailedPartitions _failedPartitionsState;
    protected ObserverId _observerId => "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    protected ObserverKey _observerKey => new(_observerId, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
    protected TestKitSilo _silo = new();
    protected IStorage<ObserverState> _stateStorage;
    protected TestStorageStats _storageStats => _silo.StorageStats<Observer, ObserverState>()!;
    protected IStorage<FailedPartitions> _failedPartitionsStorage;
    protected TestStorageStats _failedPartitionsStorageStats => _silo.StorageManager.GetStorageStats(nameof(FailedPartition))!;

    async Task Establish()
    {
        _subscriber = Substitute.For<IObserverSubscriber>();
        _silo.AddProbe((_) => _subscriber);

        _failedPartitionsState = Substitute.For<FailedPartitions>();

        _observerServiceClient = Substitute.For<IObserverServiceClient>();
        _silo.AddService(_observerServiceClient);

        var logger = _silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        _stateStorage = _silo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName);
        _failedPartitionsStorage = _silo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        _failedPartitionsStorage.State = _failedPartitionsState;

        var eventSequence = Substitute.For<IEventSequence>();
        _silo.AddProbe((key) => eventSequence);

        eventSequence.GetTailSequenceNumber().Returns(EventSequenceNumber.Unavailable);
        eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);

        _observer = await _silo.CreateGrainAsync<Observer>(_observerKey);

        _storageStats.ResetCounts();
        _failedPartitionsStorageStats.ResetCounts();
    }
}
