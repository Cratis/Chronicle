// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Specifications;
using Microsoft.Extensions.Logging;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.given;

public class a_recover_failed_partition_worker : GrainSpecification<RecoverFailedPartitionState>
{
    protected Mock<IEventSequenceStorage> event_sequence_storage_provider;
    protected Mock<IObserverSupervisor> supervisor;
    protected Mock<IObserverSubscriber> subscriber;

    internal List<TimerSettings> timers = new();

    protected override Guid GrainId => state.ObserverId;

    protected static MicroserviceId MicroserviceId { get; } = Guid.NewGuid();
    protected static TenantId TenantId { get; } = Guid.NewGuid();
    protected static EventSequenceId EventSequenceId { get; } = Guid.NewGuid();
    protected static ObserverId ObserverId { get; } = Guid.NewGuid();
    protected static EventSourceId EventSourceId { get; } = Guid.NewGuid();
    protected ObserverKey ObserverKey { get; } = new(MicroserviceId, TenantId, EventSequenceId);

    protected PartitionedObserverKey PartitionedObserverKey { get; } = new(MicroserviceId, TenantId, EventSequenceId, EventSourceId);
    protected ObserverSubscriberKey SubscriberKey = new(MicroserviceId, TenantId, EventSequenceId, EventSourceId);
    protected override string GrainKeyExtension => PartitionedObserverKey;

    protected virtual IEnumerable<AppendedEvent> events => Enumerable.Empty<AppendedEvent>();

    protected virtual RecoverFailedPartitionState BuildState() => new()
    {
        ObserverId = ObserverId
    };

    protected virtual Task<ObserverSubscriberResult> ProcessEvent(AppendedEvent evt) => Task.FromResult(ObserverSubscriberResult.Ok);

    protected virtual Task<IEventCursor> FetchEvents(EventSequenceNumber sequenceNumber) => Task.FromResult<IEventCursor>(new EventCursorForSpecifications(events));

    protected override Grain GetGrainInstance()
    {
        state = BuildState();

        supervisor = new();
        event_sequence_storage_provider = new();

        subscriber = new();
        subscriber.Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>())).Returns((AppendedEvent evt, ObserverSubscriberContext _) => ProcessEvent(evt));

        return new RecoverFailedPartition(
            Mock.Of<IExecutionContextManager>(),
            () => event_sequence_storage_provider.Object,
            Mock.Of<ILogger<RecoverFailedPartition>>());
    }

    protected override void OnBeforeGrainActivate()
    {
        grain_factory.Setup(_ => _.GetGrain(subscriber.Object.GetType(), GrainId, IsAny<string>())).Returns(subscriber.Object);
        grain_factory.Setup(_ => _.GetGrain<IObserverSupervisor>(IsAny<Guid>(), IsAny<string>(), IsAny<string>())).Returns(supervisor.Object);

        event_sequence_storage_provider
            .Setup(_ => _.GetFromSequenceNumber(IsAny<EventSequenceId>(), IsAny<EventSequenceNumber>(),
                IsAny<EventSourceId>(), IsAny<IEnumerable<EventType>>()))
            .Returns((EventSequenceId _, EventSequenceNumber sequenceNumber, EventSourceId? __, IEnumerable<EventType>? ___) =>
                FetchEvents(sequenceNumber)
            );

        timer_registry
            .Setup(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()))
            .Returns((IGrainContext _, Func<object, Task> callback, object state, TimeSpan wait, TimeSpan repeat) =>
            {
                timers.Add(new(wait, repeat));
                callback(state);
                return Task.CompletedTask;
            });

        supervisor.Setup(_ => _.GetCurrentSubscription()).Returns(() => Task.FromResult(new ObserverSubscription(GrainId, ObserverKey, Enumerable.Empty<EventType>(), subscriber.Object.GetType(), new())));
    }

    internal record TimerSettings(TimeSpan Wait, TimeSpan Repeat);
}
