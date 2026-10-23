// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.in_replay;

public class and_resuming_after_a_crash_mid_page : given.a_job_step_with_a_replay_event_interval
{
    bool _failing = true;
    EventSequenceNumber _durableAfterCrash;
    List<EventSequenceNumber> _secondRun = [];

    void Establish()
    {
        SetupPages(AlternatingEvents(1, 5));
        _observerSubscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.ArgAt<IEnumerable<AppendedEvent>>(1).ToArray();
                var number = events[0].Context.SequenceNumber;
                if (_failing && number == (EventSequenceNumber)3UL)
                {
                    return Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "boom"));
                }

                _handledBatches.Add(new(call.Arg<Key>(), events.Select(_ => _.Context.SequenceNumber).ToArray()));
                if (!_failing) _secondRun.Add(number);
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
    }

    async Task Because()
    {
        await _jobStep.InvokePerformStep(_performState);
        _durableAfterCrash = _performState.LastSuccessfullyHandledEventSequenceNumber;

        _failing = false;
        _handledBatches.Clear();
        SetupPages(AlternatingEvents(3, 5));
        await _jobStep.InvokePerformStep(_performState);
    }

    [Fact] void should_have_made_the_interval_durable_before_the_crash() => _durableAfterCrash.ShouldEqual((EventSequenceNumber)2UL);
    [Fact] void should_resume_after_the_durable_checkpoint() => _secondRun[0].ShouldEqual((EventSequenceNumber)3UL);
    [Fact] void should_not_redeliver_events_before_the_durable_checkpoint() => string.Join(',', _secondRun.Select(number => number.Value)).ShouldEqual("3,4,5");
    [Fact] void should_end_with_everything_durable() => _performState.LastSuccessfullyHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5UL);
}
