// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// The interleaving of live delivery and catch-up for one partition. The partition's step has read its last event when
/// event 11 is appended for it: live delivery drops it, as the partition is still catching up, and concluding finds it,
/// so the step reads on and delivers it. Once the step has read it, the partition concludes and the next event, 12, is
/// delivered live. Every event reaches the subscriber exactly once - live delivery never delivers what the step reads,
/// and nothing is left to nobody (#4583).
/// </summary>
public class when_an_event_arrives_for_a_partition_after_its_catch_up_step_read_its_last : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "partition";
    static readonly EventSequenceNumber _nextUnreadByTheStep = 10UL;
    static readonly EventSequenceNumber _appendedAfterStepReadItsLast = 11UL;
    static readonly EventSequenceNumber _appendedAfterConcluding = 12UL;

    readonly List<EventSequenceNumber> _deliveredLive = [];
    bool _concludedWhileTheEventWasUnread;
    bool _concludedOnceTheStepReadIt;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        _eventSequence
            .GetNextSequenceNumberGreaterOrEqualTo(_nextUnreadByTheStep, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(_appendedAfterStepReadItsLast);
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _deliveredLive.AddRange(events.Select(_ => _.Context.SequenceNumber));
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because()
    {
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _appendedAfterStepReadItsLast)]);
        _concludedWhileTheEventWasUnread = await _observer.ConcludePartitionCatchUp(_partition, _nextUnreadByTheStep, [event_type]);

        // The step has read on and handled event 11 itself.
        _concludedOnceTheStepReadIt = await _observer.ConcludePartitionCatchUp(_partition, _appendedAfterStepReadItsLast.Next(), [event_type]);
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _appendedAfterConcluding)]);
    }

    [Fact] void should_leave_the_event_appended_after_the_step_read_its_last_to_the_step() => _concludedWhileTheEventWasUnread.ShouldBeFalse();
    [Fact] void should_not_deliver_the_event_the_step_reads_live() => _deliveredLive.ShouldNotContain(_appendedAfterStepReadItsLast);
    [Fact] void should_conclude_once_the_step_has_read_it() => _concludedOnceTheStepReadIt.ShouldBeTrue();
    [Fact] void should_deliver_the_next_event_live_exactly_once() => _deliveredLive.ShouldContainOnly(_appendedAfterConcluding);
}
