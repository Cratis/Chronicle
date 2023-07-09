// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Specifications;
using Aksio.Execution;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.when_recovering;

public class with_each_event_failing_first_time_then_succeeding : given.a_recover_failed_partition_worker
{
    EventSequenceNumber current;
    EventType event_type;
    List<AppendedEvent> appended_events;
    EventSequenceNumber initial_error;
    Dictionary<EventSequenceNumber, int> countOfAttempts = new();

    protected override IEnumerable<AppendedEvent> events => appended_events;

    protected override Task<ObserverSubscriberResult> ProcessEvent(AppendedEvent evt)

    {
        if (countOfAttempts[evt.Metadata.SequenceNumber] != 0) return Task.FromResult(ObserverSubscriberResult.Ok);
        countOfAttempts[evt.Metadata.SequenceNumber] = 1;
        return Task.FromResult(ObserverSubscriberResult.Failed);
    }

    protected override Task<IEventCursor> FetchEvents(EventSequenceNumber sequenceNumber)
    {
        return Task.FromResult(new EventCursorForSpecifications(appended_events.Where(_ => _.Metadata.SequenceNumber >= sequenceNumber).ToList()) as IEventCursor);
    }

    AppendedEvent BuildAppendedEvent(EventSourceId eventSourceId)
    {
        var @event = new AppendedEvent(
            new(current, event_type),
            new(eventSourceId, current, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, PartitionedObserverKey.TenantId, CorrelationId.New(), CausationId.System, CausedBy.System),
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

        foreach (var evt in appended_events)
        {
            countOfAttempts[evt.Metadata.SequenceNumber] = 0;
        }
    }

    Task Because() => (grain as RecoverFailedPartition).Recover(ObserverKey, string.Empty, initial_error, Enumerable.Empty<EventType>(), Enumerable.Empty<string>(), string.Empty);

    [Fact]
    void should_call_the_subscriber_for_each_successful_event_twice()
    {
        foreach (var @event in appended_events)
            subscriber.Verify(_ => _.OnNext(@event, IsAny<ObserverSubscriberContext>()), Exactly(2));
    }

    [Fact] void should_persist_the_state_on_activation_and_after_each_event_is_processed() => written_states.Count.ShouldEqual(11);

    [Fact] void should_record_the_total_number_of_attempts_on_the_state() => most_recent_written_state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(5);

    [Fact] void should_notify_the_supervisor_that_the_recovery_is_completed_with_the_last_processed_event() => supervisor.Verify(_ => _.NotifyFailedPartitionRecoveryComplete(PartitionedObserverKey.EventSourceId, state.NextSequenceNumberToProcess - 1));

    [Fact] void should_retrieve_the_events_to_process_from_the_event_sequence_storage_provider() => event_sequence_storage_provider.Verify(_ => _.GetFromSequenceNumber(PartitionedObserverKey.EventSequenceId, IsAny<EventSequenceNumber>(), PartitionedObserverKey.EventSourceId, IsAny<IEnumerable<EventType>>()), Exactly(6));

    [Fact] void should_schedule_additional_timers_on_each_failure() => timer_registry.Verify(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Exactly(6));

    [Fact] void should_have_scheduled_the_immediate_timer_to_start_recovery() => timers[0].Wait.ShouldEqual(TimeSpan.Zero);
}
