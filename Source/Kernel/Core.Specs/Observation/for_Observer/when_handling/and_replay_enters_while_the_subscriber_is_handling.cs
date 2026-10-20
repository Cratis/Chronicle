// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_replay_enters_while_the_subscriber_is_handling : given.an_observer_with_subscription_for_specific_event_type
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _subscriberResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseReset = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverState _beforeReplay;

    void Establish()
    {
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(_ => _subscriberResult.Task);
        _jobsManager.Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _observerHandledCountsStorage.RemoveAllFor(_observerId).Returns(_ =>
        {
            _resetStarted.TrySetResult();
            return _releaseReset.Task;
        });
    }

    async Task Because()
    {
        var handling = _observer.Handle("partition", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 42UL)]);
        _beforeReplay = _stateStorage.State;
        var replaying = _observer.Replay();
        await _resetStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _subscriberResult.SetResult(ObserverSubscriberResult.Ok(42UL));
        await handling;
        _releaseReset.SetResult();
        await replaying;
    }

    [Fact] void should_leave_replay_in_charge_of_the_next_position() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual(_beforeReplay.NextEventSequenceNumber);
    [Fact] void should_leave_replay_in_charge_of_the_last_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_beforeReplay.LastHandledEventSequenceNumber);
    [Fact] void should_leave_replay_in_charge_of_the_tail_position() => _stateStorage.State.TailEventSequenceNumber.ShouldEqual(_beforeReplay.TailEventSequenceNumber);
    [Fact] void should_reset_handled_counts() => _stateStorage.State.HandledEventCount.ShouldEqual(EventCount.Zero);
    [Fact] void should_keep_the_completed_marker_removed() => _stateStorage.State.InFlightPartitions.ShouldBeEmpty();
}
