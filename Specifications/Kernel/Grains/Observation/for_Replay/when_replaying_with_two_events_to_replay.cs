// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.for_Replay;

public class when_replaying_with_two_events_to_replay : given.a_replay_with_two_pending_events
{
    List<AppendedEvent> events_replayed;

    void Establish()
    {
        events_replayed = new();
        event_sequence_storage_provider.Setup(_ => _.GetHeadSequenceNumber(IsAny<EventSequenceId>(), event_types, null)).ReturnsAsync(first_appended_event.Metadata.SequenceNumber);
        event_sequence_storage_provider.Setup(_ => _.GetTailSequenceNumber(IsAny<EventSequenceId>(), event_types, null)).ReturnsAsync(second_appended_event.Metadata.SequenceNumber);
        state.LastHandled = second_appended_event.Metadata.SequenceNumber;
        subscriber
            .Setup(_ => _.OnNext(IsAny<IEnumerable<AppendedEvent>>(), IsAny<ObserverSubscriberContext>()))
            .Callback((IEnumerable<AppendedEvent> events, ObserverSubscriberContext _) => events_replayed.AddRange(events));
    }

    Task Because() => replay.Start(new(GrainId, ObserverKey.Parse(GrainKeyExtension), event_types, typeof(ObserverSubscriber), subscriber_args));

    [Fact] void should_replay_first_event() => events_replayed[0].Content.ShouldEqual(first_appended_event.Content);
    [Fact] void should_replay_second_event() => events_replayed[1].Content.ShouldEqual(first_appended_event.Content);
    [Fact] void should_have_head_of_replay_state_for_first_event() => events_replayed[0].Context.ObservationState.HasFlag(EventObservationState.HeadOfReplay).ShouldBeTrue();
    [Fact] void should_have_tail_of_replay_state_for_first_event() => events_replayed[1].Context.ObservationState.HasFlag(EventObservationState.TailOfReplay).ShouldBeTrue();
    [Fact] void should_notify_supervisor_that_replay_is_complete() => supervisor.Verify(_ => _.NotifyReplayComplete(IsAny<IEnumerable<FailedPartition>>()), Once);
}
