// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Text.Json;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Specifications;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.given;

public class an_observer_worker : GrainSpecification
{
    protected Mock<IPersistentState<ObserverState>> persistent_state;
    protected ObserverState state;
    protected ObserverState state_on_write;
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected Mock<IObserverSupervisor> supervisor;
    protected Mock<IObserverSubscriber> subscriber;
    protected override Guid GrainId => state.ObserverId;

    protected override string GrainKeyExtension => new ObserverKey(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());

    protected ObserverWorkerImplementation worker;
    protected virtual IEnumerable<AppendedEvent> events => Enumerable.Empty<AppendedEvent>();

    protected override Grain GetGrainInstance()
    {
        state = new()
        {
            ObserverId = Guid.NewGuid(),
        };
        persistent_state = new();
        persistent_state.Setup(_ => _.State).Returns(() => state);
        persistent_state.Setup(_ => _.WriteStateAsync()).Returns(() =>
        {
            var serialized = JsonSerializer.Serialize(state);
            state_on_write = JsonSerializer.Deserialize<ObserverState>(serialized);
            return Task.CompletedTask;
        });

        supervisor = new();
        event_sequence_storage_provider = new();
        subscriber = new();
        subscriber.Setup(_ => _.OnNext(IsAny<AppendedEvent>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok));

        worker = new ObserverWorkerImplementation(
            Mock.Of<IExecutionContextManager>(),
            () => event_sequence_storage_provider.Object,
            persistent_state.Object,
            Mock.Of<ILogger<ObserverWorker>>());

        worker.SetSubscriberType(typeof(ObserverSubscriber));

        return worker;
    }


    protected override void OnBeforeGrainActivate()
    {
        grain_factory.Setup(_ => _.GetGrain(typeof(ObserverSubscriber), state.ObserverId, IsAny<string>())).Returns(subscriber.Object);
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);

        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(), IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns(() => Task.FromResult<IEventCursor>(new EventCursorForSpecifications(events)));
    }
}
