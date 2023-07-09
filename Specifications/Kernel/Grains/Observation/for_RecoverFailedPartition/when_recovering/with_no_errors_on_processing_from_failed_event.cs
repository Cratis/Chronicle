// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Execution;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.when_recovering;

public class with_no_errors_on_processing_from_failed_event : given.a_recover_failed_partition_worker
{
    ObserverKey observer_key;
    PartitionedObserverKey partitioned_observer_key;
    EventSequenceNumber current;
    EventType event_type;
    IEnumerable<AppendedEvent> appended_events;
    EventSequenceNumber initial_error;

    protected override IEnumerable<AppendedEvent> events => appended_events;

    AppendedEvent BuildAppendedEvent(EventSourceId eventSourceId)
    {
        var @event = new AppendedEvent(
            new(current, event_type),
            new(eventSourceId, current, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, partitioned_observer_key.TenantId, CorrelationId.New(), CausationId.System, CausedBy.System),
            new ExpandoObject());
        current++;
        return @event;
    }

    void Establish()
    {
        event_type = new EventType(Guid.NewGuid(), EventGeneration.First);
        current = initial_error = 5;
        partitioned_observer_key = PartitionedObserverKey.Parse(GrainKeyExtension);
        observer_key = new(partitioned_observer_key.MicroserviceId, partitioned_observer_key.TenantId, partitioned_observer_key.EventSequenceId);
        appended_events = new List<AppendedEvent>()
        {
            BuildAppendedEvent(partitioned_observer_key.EventSourceId),
            BuildAppendedEvent(partitioned_observer_key.EventSourceId),
            BuildAppendedEvent(partitioned_observer_key.EventSourceId),
            BuildAppendedEvent(partitioned_observer_key.EventSourceId),
            BuildAppendedEvent(partitioned_observer_key.EventSourceId)
        };
    }

    Task Because() => (grain as RecoverFailedPartition).Recover(observer_key, string.Empty, initial_error, Enumerable.Empty<EventType>(), Enumerable.Empty<string>(), string.Empty);

    [Fact]
    void should_call_the_subscriber_for_each_event()
    {
        foreach (var @event in appended_events)
            subscriber.Verify(_ => _.OnNext(@event, IsAny<ObserverSubscriberContext>()), Once);
    }

    [Fact] void should_persist_the_state_on_activation_and_after_each_event_is_processed() => written_states.Count.ShouldEqual(6);

    [Fact] void should_have_recorded_the_number_of_attempts_in_the_state() => most_recent_written_state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(0);

    [Fact] void should_notify_the_supervisor_that_the_recovery_is_completed_with_the_last_processed_event() => supervisor.Verify(_ => _.NotifyFailedPartitionRecoveryComplete(partitioned_observer_key.EventSourceId, state.NextSequenceNumberToProcess - 1));

    [Fact] void should_retrieve_the_events_to_process_from_the_event_sequence_storage_provider() => event_sequence_storage_provider.Verify(_ => _.GetFromSequenceNumber(partitioned_observer_key.EventSequenceId, initial_error, partitioned_observer_key.EventSourceId, IsAny<IEnumerable<EventType>>()), Once);

    [Fact] void should_not_schedule_any_more_timers() => timer_registry.Verify(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Once);

    [Fact] void should_have_scheduled_the_immediate_timer_to_start_recovery() => timers[0].Wait.ShouldEqual(TimeSpan.Zero);
}
