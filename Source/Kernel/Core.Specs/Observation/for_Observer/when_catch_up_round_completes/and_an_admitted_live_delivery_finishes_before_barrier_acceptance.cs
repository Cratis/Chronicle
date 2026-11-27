// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_an_admitted_live_delivery_finishes_before_barrier_acceptance : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _liveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _liveResult = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "P" });
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)]);
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(_ =>
        {
            _liveStarted.TrySetResult();
            return _liveResult.Task;
        });
    }

    async Task Because()
    {
        var live = _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 16UL)]);
        await _liveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var acceptance = _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        _liveResult.TrySetResult(ObserverSubscriberResult.Ok(16UL));
        await Task.WhenAll(live, acceptance).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_freeze_at_the_final_durably_accepted_live_cursor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)17UL);
}
