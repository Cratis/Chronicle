// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Identities;
using Orleans.Runtime;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_RecoverFailedPartition.when_recovering;

public class with_initial_event_failing_first_time_then_succeeding : given.a_recover_failed_partition_worker
{
    ObserverKey observer_key;
    PartitionedObserverKey partitioned_observer_key;
    EventSequenceNumber current;
    EventType event_type;
    IEnumerable<AppendedEvent> appended_events;
    EventSequenceNumber initial_error;
    int countOfAttempts = 0;

    protected override IEnumerable<AppendedEvent> events => appended_events;

    protected override Task<ObserverSubscriberResult> ProcessEvent(AppendedEvent evt)
    {
        if (evt.Metadata.SequenceNumber != initial_error) return Task.FromResult(ObserverSubscriberResult.Ok);
        if (countOfAttempts != 0) return Task.FromResult(ObserverSubscriberResult.Ok);
        countOfAttempts++;
        return Task.FromResult(ObserverSubscriberResult.Failed(evt.Metadata.SequenceNumber));
    }

    AppendedEvent BuildAppendedEvent(EventSourceId eventSourceId)
    {
        var @event = new AppendedEvent(
            new(current, event_type),
            new(eventSourceId, current, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, partitioned_observer_key.TenantId, CorrelationId.New(), Enumerable.Empty<Causation>(), Identity.System),
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
    void should_call_the_subscriber_for_the_failed_event_twice()
    {
        foreach (var @event in appended_events.Where(_ => _.Metadata.SequenceNumber == initial_error))
            subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == @event), IsAny<ObserverSubscriberContext>()), Exactly(2));
    }

    [Fact]
    void should_call_the_subscriber_for_each_successful_event_once()
    {
        foreach (var @event in appended_events.Where(_ => _.Metadata.SequenceNumber != initial_error))
            subscriber.Verify(_ => _.OnNext(Is<IEnumerable<AppendedEvent>>(m => m.First() == @event), IsAny<ObserverSubscriberContext>()), Once);
    }

    [Fact] void should_persist_the_state_on_activation_and_after_each_event_is_processed() => written_states.Count.ShouldEqual(7);

    [Fact] void should_have_recorded_the_failed_attempt_in_the_state() => written_states[1].NumberOfAttemptsOnCurrentError.ShouldEqual(1);

    [Fact] void should_have_recorded_the_number_of_attempts_in_the_state() => most_recent_written_state.NumberOfAttemptsOnSinceInitialized.ShouldEqual(1);

    [Fact] void should_notify_the_supervisor_that_the_recovery_is_completed_with_the_last_processed_event() => supervisor.Verify(_ => _.NotifyFailedPartitionRecoveryComplete(partitioned_observer_key.EventSourceId, state.NextSequenceNumberToProcess - 1));

    [Fact] void should_retrieve_the_events_to_process_from_the_event_sequence_storage_provider() => event_sequence_storage_provider.Verify(_ => _.GetFromSequenceNumber(partitioned_observer_key.EventSequenceId, initial_error, partitioned_observer_key.EventSourceId, IsAny<IEnumerable<EventType>>()), Exactly(2));

    [Fact] void should_schedule_an_additional_timer() => timer_registry.Verify(_ => _.RegisterTimer(IsAny<IGrainContext>(), IsAny<Func<object, Task>>(), IsAny<object>(), IsAny<TimeSpan>(), IsAny<TimeSpan>()), Exactly(2));

    [Fact] void should_have_scheduled_the_immediate_timer_to_start_recovery() => timers[0].Wait.ShouldEqual(TimeSpan.Zero);
}
