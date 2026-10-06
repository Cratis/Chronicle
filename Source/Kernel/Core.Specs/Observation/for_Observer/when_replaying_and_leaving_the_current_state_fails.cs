// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using AppendedEventsQueueSubscription = Cratis.Chronicle.EventSequences.AppendedEventsQueueSubscription;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// A replay request that fails before the observer reaches the replay state has been answered with that failure. It
/// must not linger and turn a later, unrelated arrival in Observing into a replay nobody asked for
/// (Cratis/Chronicle#4514).
/// </summary>
public class when_replaying_and_leaving_the_current_state_fails : given.an_observer_with_subscription
{
    Exception _error;

    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);
        _appendedEventsQueues
            .Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>())
            .Returns(_ => throw new InvalidOperationException("Unsubscribing failed"), _ => Task.CompletedTask);
    }

    async Task Because()
    {
        _error = await Catch.Exception(_observer.Replay);
        await _observer.TransitionTo<Routing>();
    }

    [Fact] void should_fail_the_replay_request() => _error.ShouldNotBeNull();
    [Fact] void should_only_start_the_replay_job_it_was_asked_for() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_not_be_replaying() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
