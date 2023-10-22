// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_ObserverWorker.given;

public class an_observer_worker : GrainSpecification
{
    protected Mock<IPersistentState<ObserverState>> persistent_state;
    protected ObserverState state;
    protected ObserverState state_on_write;
    protected Mock<IObserverSupervisor> supervisor;
    protected Mock<IObserverSubscriber> subscriber;
    protected override Guid GrainId => state.ObserverId;

    protected override string GrainKeyExtension => new ObserverKey(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());

    protected ObserverWorkerImplementation worker;
    protected virtual IEnumerable<AppendedEvent> events => Enumerable.Empty<AppendedEvent>();
    protected Mock<EventSequences.IEventSequence> event_sequence;

    protected override Grain GetGrainInstance()
    {
        event_sequence = new();

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
        subscriber = new();
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok));

        worker = new ObserverWorkerImplementation(
            Mock.Of<IExecutionContextManager>(),
            persistent_state.Object,
            Mock.Of<ILogger<ObserverWorker>>());

        worker.SetCurrentSubscription(new(GrainId, ObserverKey.Parse(GrainKeyExtension), Enumerable.Empty<EventType>(), typeof(ObserverSubscriber), null!));

        return worker;
    }


    protected override void OnBeforeGrainActivate()
    {
        grain_factory.Setup(_ => _.GetGrain<EventSequences.IEventSequence>(IsAny<Guid>(), IsAny<string>(), null!)).Returns(event_sequence.Object);
        grain_factory.Setup(_ => _.GetGrain(typeof(ObserverSubscriber), state.ObserverId, IsAny<string>())).Returns(subscriber.Object);
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);
    }
}
