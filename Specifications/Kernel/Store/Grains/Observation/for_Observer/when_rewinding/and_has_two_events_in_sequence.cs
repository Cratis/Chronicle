// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation.for_Observer.when_rewinding;

public class and_has_two_events_in_sequence : given.an_observer_and_two_event_types
{
    EventSourceId event_source_id;
    AppendedEvent first_appended_event;
    AppendedEvent second_appended_event;

    List<AppendedEvent> appended_events;

    async Task Establish()
    {
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_types, null)).Returns(Task.FromResult((EventSequenceNumber)1));
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(event_types, event_source_id)).Returns(Task.FromResult((EventSequenceNumber)2));

        await observer.Subscribe(event_types, observer_namespace);
        state.RunningState = ObserverRunningState.Active;

        state.NextEventSequenceNumber = EventSequenceNumber.First + 2;

        event_source_id = Guid.NewGuid().ToString();

        storage.Invocations.Clear();

        first_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First, event_types.ToArray()[0]),
            new(event_source_id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        second_appended_event = new AppendedEvent(
            new(EventSequenceNumber.First + 1, event_types.ToArray()[0]),
            new(event_source_id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, TenantId.Development, CorrelationId.New(), CausationId.System, CausedBy.System),
            new JsonObject());

        state.LastHandled = EventSequenceNumber.First + 1;
        appended_events = new();
        observer_stream.Setup(_ => _.OnNextAsync(
            IsAny<AppendedEvent>(),
            IsAny<StreamSequenceToken>())).Returns(
                (AppendedEvent @event, StreamSequenceToken _) =>
                {
                    appended_events.Add(@event);
                    return Task.CompletedTask;
                });
    }

    async Task Because()
    {
        await observer.Rewind();
        await observers[1].OnNextAsync(first_appended_event);
        await observers[1].OnNextAsync(second_appended_event);
    }

    [Fact] void should_set_replay_as_event_observation_state_for_first_event() => appended_events[0].Context.ObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] void should_set_tail_of_replay_as_event_observation_state_for_second_event() => appended_events[1].Context.ObservationState.ShouldEqual(EventObservationState.TailOfReplay);
}
