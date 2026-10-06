// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

/// <summary>
/// Catch-up reporting back interleaves with live delivery. While the subscriber is handling event 50 of a partition
/// that is not catching up, the catch-up reports back having got to 100 and moves the position to 101. Once the
/// subscriber is done, the position must stay at 101: moving it back to 51 would have the next catch-up deliver
/// everything from 51 to 100 a second time (#4583).
/// </summary>
public class and_catch_up_moves_the_position_past_it_while_the_subscriber_handles_it : given.an_observer_with_subscription_for_specific_event_type
{
    static readonly EventSequenceNumber _handledLive = 50UL;
    static readonly EventSequenceNumber _caughtUpTo = 100UL;

    readonly TaskCompletionSource<ObserverSubscriberResult> _subscriberHandling = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 5UL };
        _subscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _subscriberHandling.Task);
    }

    async Task Because()
    {
        var handling = _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, _handledLive)]);
        await _observer.CaughtUp(JobId.New(), _caughtUpTo);
        _subscriberHandling.SetResult(ObserverSubscriberResult.Ok(_handledLive));
        await handling;
    }

    [Fact] void should_not_move_the_position_back() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_caughtUpTo.Next());
    [Fact] void should_keep_the_furthest_handled_event_as_the_last_handled() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_caughtUpTo);
}
