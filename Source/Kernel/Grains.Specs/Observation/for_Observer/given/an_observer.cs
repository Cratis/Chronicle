// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Observation;
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
    protected Observer observer;
    protected Mock<IStreamProvider> stream_provider;
    protected Mock<IStreamProvider> sequence_stream_provider;
    protected Mock<IObserverSubscriber> subscriber;
    protected Mock<IObserverServiceClient> observer_service_client;
    protected FailedPartitions failed_partitions_state;
    protected ObserverId observer_id => "d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e";
    protected ObserverKey observer_key => new(observer_id, EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
    protected TestKitSilo silo = new();
    protected IStorage<ObserverState> state_storage;
    protected TestStorageStats storage_stats => silo.StorageStats<Observer, ObserverState>()!;
    protected IStorage<FailedPartitions> failed_partitions_storage;
    protected TestStorageStats failed_partitions_storage_stats => silo.StorageManager.GetStorageStats(nameof(FailedPartition))!;

    async Task Establish()
    {
        subscriber = new();
        silo.AddProbe((_) => subscriber.Object);

        failed_partitions_state = new();

        observer_service_client = silo.AddServiceProbe<IObserverServiceClient>();

        var logger = silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = silo.AddServiceProbe<ILoggerFactory>();
        loggerFactory.Setup(_ => _.CreateLogger(IsAny<string>())).Returns(logger);

        state_storage = silo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName);
        failed_partitions_storage = silo.StorageManager.GetStorage<FailedPartitions>(nameof(FailedPartition));
        failed_partitions_storage.State = failed_partitions_state;

        var eventSequence = silo.AddProbe<IEventSequence>(
            new EventSequenceKey(observer_key.EventSequenceId, observer_key.EventStore, observer_key.Namespace));

        eventSequence.Setup(_ => _.GetTailSequenceNumber()).ReturnsAsync(EventSequenceNumber.Unavailable);
        eventSequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(IsAny<IEnumerable<EventType>>())).ReturnsAsync(EventSequenceNumber.Unavailable);

        observer = await silo.CreateGrainAsync<Observer>(observer_key);

        storage_stats.ResetCounts();
        failed_partitions_storage_stats.ResetCounts();
    }
}
