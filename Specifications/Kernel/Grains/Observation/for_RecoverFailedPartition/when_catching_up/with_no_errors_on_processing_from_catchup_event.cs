// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.when_catching_up;

public class with_no_errors_on_processing_from_catchup_event : given.an_initiated_recover_failed_partition_worker
{
    EventSequenceNumber current;
    EventType event_type;
    IEnumerable<AppendedEvent> appended_events;
    EventSequenceNumber initial_error;

    protected override IEnumerable<AppendedEvent> events => appended_events;

    AppendedEvent BuildAppendedEvent(EventSourceId eventSourceId)
    {
        var @event = new AppendedEvent(
            new(current, event_type),
            new(eventSourceId, current, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, PartitionedObserverKey.TenantId, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System),
            new ExpandoObject());
        current++;
        return @event;
    }

    void Establish()
    {
        event_type = new EventType(Guid.NewGuid(), EventGeneration.First);
        current = initial_error = 5;
        appended_events = new List<AppendedEvent>()
        {
            BuildAppendedEvent(PartitionedObserverKey.EventSourceId),
            BuildAppendedEvent(PartitionedObserverKey.EventSourceId),
            BuildAppendedEvent(PartitionedObserverKey.EventSourceId),
            BuildAppendedEvent(PartitionedObserverKey.EventSourceId),
            BuildAppendedEvent(PartitionedObserverKey.EventSourceId)
        };
    }

    async Task Because() => await (grain as RecoverFailedPartition).Catchup(initial_error);

    [Fact]
    void should_call_the_subscriber_for_each_event()
    {
        foreach (var @event in appended_events)
            subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == @event), IsAny<ObserverSubscriberContext>()), Once);
    }

    [Fact] void should_persist_the_state_on_activation_and_after_each_event_is_processed() => written_states.Count.ShouldEqual(6);

    [Fact] void should_have_recorded_the_number_of_attempts_in_the_state() => most_recent_written_state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(0);

    [Fact] void should_notify_the_supervisor_that_the_recovery_is_completed_with_the_last_processed_event() => supervisor.Verify(_ => _.NotifyFailedPartitionRecoveryComplete(PartitionedObserverKey.EventSourceId, state.NextSequenceNumberToProcess - 1));

    [Fact] void should_retrieve_the_events_to_process_from_the_event_sequence_storage_provider() => event_sequence_storage_provider.Verify(_ => _.GetFromSequenceNumber(PartitionedObserverKey.EventSequenceId, initial_error, PartitionedObserverKey.EventSourceId, IsAny<IEnumerable<EventType>>()), Once);

    [Fact] void should_not_schedule_any_more_timers() => timer_registry.Verify(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Once);

    [Fact] void should_have_scheduled_the_immediate_timer_to_start_recovery() => timers[0].Wait.ShouldEqual(TimeSpan.Zero);
}
