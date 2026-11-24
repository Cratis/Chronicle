// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_relevant_event_was_missed_before_the_live_batch : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(102UL, Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)102UL);
        _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>())
            .Returns(Result<JobId, StartJobError>.Success(JobId.New()));
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(ObserverSubscriberResult.Ok(105UL));
    }

    async Task Because()
    {
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 106UL)]);
    }

    [Fact] void should_keep_the_cursor_at_the_missed_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_not_deliver_past_the_gap() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_start_one_catch_up_from_the_cursor() => _jobsManager.Received(1).Start<ICatchUpObserver, CatchUpObserverRequest>(
        Arg.Is<CatchUpObserverRequest>(_ => _.ObserverKey == _observerKey && _.FromEventSequenceNumber == (EventSequenceNumber)102UL));
}
