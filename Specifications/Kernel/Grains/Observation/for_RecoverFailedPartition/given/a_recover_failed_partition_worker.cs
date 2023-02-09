// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Specifications;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.given;

public class a_recover_failed_partition_worker : GrainSpecification<RecoverFailedPartitionState>
{
    protected Mock<IEventSequenceStorageProvider> event_sequence_storage_provider;
    protected Mock<IObserverSupervisor> supervisor;
    protected Mock<IObserverSubscriber> subscriber;

    internal List<TimerSettings> timers = new();

    protected override Guid GrainId => state.ObserverId;

    PartitionedObserverKey _partitionedObserverKey = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid());
    protected override string GrainKeyExtension => _partitionedObserverKey;

    protected virtual IEnumerable<AppendedEvent> events => Enumerable.Empty<AppendedEvent>();
    
    protected virtual Task<ObserverSubscriberResult> ProcessEvent(AppendedEvent evt) => Task.FromResult(ObserverSubscriberResult.Ok);
    
    protected virtual Task<IEventCursor> FetchEvents(EventSequenceNumber sequenceNumber) => Task.FromResult<IEventCursor>(new EventCursorForSpecifications(events));
    
    protected override Grain GetGrainInstance()
    {
        state = new()
        {
            ObserverId = Guid.NewGuid(),
        };

        supervisor = new();
        event_sequence_storage_provider = new();

        subscriber = new();
        subscriber.Setup(_ => _.OnNext(IsAny<AppendedEvent>())).Returns((AppendedEvent evt) => ProcessEvent(evt));

        var recover = new RecoverFailedPartition(
            Mock.Of<IExecutionContextManager>(),
            () => event_sequence_storage_provider.Object,
            Mock.Of<ILogger<RecoverFailedPartition>>());
        return recover;
    }

   

    protected override void OnBeforeGrainActivate()
    {
        grain_factory.Setup(_ => _.GetGrain(subscriber.Object.GetType(), GrainId, IsAny<string>())).Returns(subscriber.Object);
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);
        
        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(),
                IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns((EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId, IEnumerable<EventType>? eventTypes) =>
                FetchEvents(sequenceNumber)
            );

        timer_registry
            .Setup(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((Grain _, Func<object, Task> callback, object state, TimeSpan wait, TimeSpan repeat) =>
            {
                timers.Add(new(wait, repeat));
                callback(state);
                return Task.CompletedTask;
            });
        
        supervisor.Setup(_ => _.GetSubscriberType()).Returns(() => Task.FromResult(subscriber.Object.GetType()));
    }

    internal record TimerSettings(TimeSpan Wait, TimeSpan Repeat);
}