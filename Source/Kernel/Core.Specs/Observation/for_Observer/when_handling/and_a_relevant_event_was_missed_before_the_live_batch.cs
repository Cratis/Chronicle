// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_relevant_event_was_missed_before_the_live_batch : given.an_observer_with_subscription_for_specific_event_type
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 102UL };
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)106UL);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverRequest(_observerKey, ObserverType.Projection, 102UL, [event_type])
        });
    }

    async Task Because()
    {
        // The queue closes live delivery and requests normal catch-up when its filtered boundary scan finds
        // the gap. Even adoption of an existing job must hold later live events below the unchanged cursor.
        await _observer.CatchUp();
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 105UL)]);
        await _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 106UL)]);
    }

    [Fact] void should_keep_the_cursor_at_the_missed_event() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)102UL);
    [Fact] void should_not_deliver_past_the_gap() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_start_a_duplicate_job() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_find_the_existing_job() => _jobsManager.Received().GetJobs(Arg.Any<JobQuery>());
}
