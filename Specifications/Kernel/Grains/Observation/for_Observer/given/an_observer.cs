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
    protected Mock<IPersistentState<FailedPartitions>> failed_partitions_persistent_state;
    protected Mock<IObserverServiceClient> observer_service_client;
    protected List<FailedPartitions> written_failed_partitions_states = [];
    protected FailedPartitions failed_partitions_state;
    protected ObserverId observer_id => Guid.Parse("d2a138a2-6ca5-4bff-8a2f-ffd8534cc80e");
    protected ObserverKey observer_key => new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSequenceId.Log);
    protected TestKitSilo silo = new();
    protected IStorage<ObserverState> state_storage;
    protected TestStorageStats storage_stats => silo.StorageStats<Observer, ObserverState>();

    async Task Establish()
    {
        subscriber = new();
        silo.AddProbe((_) => subscriber.Object);

        failed_partitions_persistent_state = silo.AddServiceProbe<IPersistentState<FailedPartitions>>();
        failed_partitions_state = new();
        failed_partitions_persistent_state.SetupGet(_ => _.State).Returns(failed_partitions_state);

        var jsonSerializerOptions = new JsonSerializerOptions(Globals.JsonSerializerOptions);
        jsonSerializerOptions.Converters.Add(new KeyJsonConverter());

        failed_partitions_persistent_state.Setup(_ => _.WriteStateAsync()).Callback(() =>
            {
                var serialized = JsonSerializer.Serialize(failed_partitions_state, jsonSerializerOptions);
                var clone = JsonSerializer.Deserialize<FailedPartitions>(serialized, jsonSerializerOptions);
                written_failed_partitions_states.Add(clone);
            }).Returns(Task.CompletedTask);

        observer_service_client = silo.AddServiceProbe<IObserverServiceClient>();

        var logger = silo.AddService(NullLogger<Observer>.Instance);
        var loggerFactory = silo.AddServiceProbe<ILoggerFactory>();
        loggerFactory.Setup(_ => _.CreateLogger(IsAny<string>())).Returns(logger);

        var mapper = new Mock<IAttributeToFactoryMapper<PersistentStateAttribute>>();
        mapper.Setup(_ => _.GetFactory(IsAny<ParameterInfo>(), IsAny<PersistentStateAttribute>())).Returns(
            context => failed_partitions_persistent_state.Object);

        silo.AddService(mapper.Object);

        state_storage = silo.StorageManager.GetStorage<ObserverState>(typeof(Observer).FullName);

        var eventSequence = silo.AddProbe<IEventSequence>(
            observer_key.EventSequenceId,
            new EventSequenceKey(observer_key.EventStore, observer_key.Namespace));

        eventSequence.Setup(_ => _.GetTailSequenceNumber()).ReturnsAsync(EventSequenceNumber.Unavailable);
        eventSequence.Setup(_ => _.GetTailSequenceNumberForEventTypes(IsAny<IEnumerable<EventType>>())).ReturnsAsync(EventSequenceNumber.Unavailable);

        observer = await silo.CreateGrainAsync<Observer>(observer_id, observer_key);

        storage_stats.ResetCounts();
    }
}
