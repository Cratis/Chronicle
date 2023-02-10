// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Dynamic;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Specifications;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.when_recovering;

public class with_each_event_failing_first_time_then_succeeding : given.a_recover_failed_partition_worker
{
    ObserverKey observer_key;
    PartitionedObserverKey partitioned_observer_key;
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

    protected override Task<IEventCursor> FetchEvents(EventSequenceNumber seq)
    {
        return Task.FromResult(new EventCursorForSpecifications(appended_events.Where(_ => _.Metadata.SequenceNumber >= seq).ToList()) as IEventCursor);
    }
    
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
        
        foreach (var evt in appended_events)
        {
            countOfAttempts[evt.Metadata.SequenceNumber] = 0;
        }
    }

    async Task Because()
    {
        await (grain as RecoverFailedPartition).Recover(initial_error, new List<EventType>(), observer_key);
        Debug.WriteLine("Done");
    }
    
    [Fact]
    void should_call_the_subscriber_for_each_successful_event_twice()
    {
        foreach (var @event in appended_events) 
            subscriber.Verify(_ => _.OnNext(@event, IsAny<ObserverSubscriberContext>()), Exactly(2));
    }
    
    [Fact]
    void should_persist_the_state_on_activation_and_after_each_event_is_processed() => written_states.Count.ShouldEqual(11);
    
    [Fact]
    void should_record_the_total_number_of_attempts_on_the_state() => most_recent_written_state.NumberOfAttemptsOnSinceInitialised.ShouldEqual(5);
    
    [Fact]
    void should_notify_the_supervisor_that_the_recovery_is_completed_with_the_last_processed_event() 
        => supervisor.Verify(_ => _.NotifyFailedPartitionRecoveryComplete(state.NextSequenceNumberToProcess - 1));
    
    [Fact] void should_retrieve_the_events_to_process_from_the_event_sequence_storage_provider()
        => event_sequence_storage_provider.Verify(_ => _.GetFromSequenceNumber(partitioned_observer_key.EventSequenceId, IsAny<EventSequenceNumber>(), partitioned_observer_key.EventSourceId, IsAny<IEnumerable<EventType>>()), Exactly(6));

    [Fact] void should_schedule_additional_timers_on_each_failure() => timer_registry.Verify(_ => _.RegisterTimer(grain, IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Exactly(6));
    
    [Fact] void should_have_scheduled_the_immediate_timer_to_start_recovery() => timers.First().Wait.ShouldEqual(TimeSpan.Zero);
}