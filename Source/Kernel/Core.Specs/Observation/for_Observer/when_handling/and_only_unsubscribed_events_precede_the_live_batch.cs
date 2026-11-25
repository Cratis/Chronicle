// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_only_unsubscribed_events_precede_the_live_batch : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(102UL, Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)105UL);
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call => ObserverSubscriberResult.Ok(call.Arg<IEnumerable<AppendedEvent>>().Last().Context.SequenceNumber));
        _jobsManager.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 110UL)]);
    }

    [Fact] void should_deliver_the_batch() => _subscriber.Received(2).OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_advance_the_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)111UL);
    [Fact] void should_not_look_up_jobs_per_batch() => _jobsManager.DidNotReceive().GetJobs(Arg.Any<JobQuery>());
    [Fact] void should_not_read_the_event_sequence_per_batch() => _eventSequence.DidNotReceive().GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_not_start_catch_up() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
}
