// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A catch-up step reports handled events while the replay state is resetting the handled counts. The observer had
/// no counts when the transition began, so the reset leaves the counts as handed - by value and by reference - and
/// the counts reported meanwhile must still be reset along with everything else (#4972).
/// </summary>
public class when_replaying_and_events_are_reported_during_the_transition : given.an_observer_with_subscription
{
    static readonly EventTypeId _eventTypeId = "d9a13e10-21a4-4cfc-896e-fda8dfeb79bb";
    static readonly Key _partition = "partition";

    TaskCompletionSource _resetStarted;
    TaskCompletionSource _resetRelease;

    async Task Establish()
    {
        _resetStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _resetRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _jobsManager
            .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        _observerHandledCountsStorage.RemoveAllFor(_observerId).Returns(_ =>
        {
            _resetStarted.TrySetResult();
            return _resetRelease.Task;
        });
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
    }

    async Task Because()
    {
        var replaying = _observer.Replay();
        await _resetStarted.Task;

        await _observer.ReportHandledEvents(_partition, new Dictionary<EventTypeId, EventCount> { [_eventTypeId] = 100UL }, JobStepId.New(), 1UL);

        _resetRelease.SetResult();
        await replaying;
    }

    [Fact] void should_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Replaying);
    [Fact] void should_reset_the_handled_count() => _stateStorage.State.HandledEventCount.ShouldEqual(EventCount.Zero);
    [Fact] void should_reset_the_handled_count_for_the_event_type() => _stateStorage.State.HandledEventCountPerEventType.ShouldBeEmpty();
}
