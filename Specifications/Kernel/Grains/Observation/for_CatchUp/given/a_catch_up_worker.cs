// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Specifications;
using Aksio.Execution;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_CatchUp.given;

public class a_catch_up_worker : GrainSpecification
{
    protected Mock<IPersistentState<ObserverState>> persistent_state;
    protected ObserverState state;
    protected ObserverState state_on_write;
    protected CatchUp catch_up;

    protected Mock<IEventSequenceStorage> event_sequence_storage_provider;
    protected Mock<IObserverSupervisor> supervisor;
    protected Mock<IObserverSubscriber> subscriber;

    protected object subscriber_args;

    protected override Guid GrainId => state.ObserverId;

    protected override string GrainKeyExtension => new ObserverKey(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());

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
        subscriber.Setup(_ => _.OnNext(IsAny<AppendedEvent>(), IsAny<ObserverSubscriberContext>())).Returns(Task.FromResult(ObserverSubscriberResult.Ok));

        catch_up = new CatchUp(
            Mock.Of<IExecutionContextManager>(),
            () => event_sequence_storage_provider.Object,
            persistent_state.Object,
            Mock.Of<ILogger<CatchUp>>());

        return catch_up;
    }

    protected override void OnBeforeGrainActivate()
    {
        subscriber_args = Guid.NewGuid();

        grain_factory.Setup(_ => _.GetGrain(typeof(ObserverSubscriber), state.ObserverId, IsAny<string>())).Returns(subscriber.Object);
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);

        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(), IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns(() => Task.FromResult<IEventCursor>(new EventCursorForSpecifications(events)));

        timer_registry
            .Setup(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain __, Func<object, Task> callback, object state, TimeSpan ___, TimeSpan ____) =>
            {
                callback(state);
                return Task.CompletedTask;
            });
    }
}
