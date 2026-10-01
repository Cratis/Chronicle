// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// An observer quarantined in the middle of a replay is still replaying when it is subscribed again. Leaving the
/// quarantine must leave that to the subscription, so the replay is picked up once, for the event types being
/// subscribed to, rather than by a pass that runs before the subscription is in place.
/// </summary>
public class and_the_quarantined_observer_was_reactivated_while_replaying : given.a_reactivated_quarantined_observer
{
    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _observerHandledCountsStorage.ClearReceivedCalls();
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_be_replaying() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_still_be_marked_as_replaying() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_enter_replay_once() => _observerHandledCountsStorage.Received(1).RemoveAllFor(Arg.Any<ObserverId>());

    [Fact]
    void should_start_the_replay_for_the_subscribed_event_types() => _jobsManager
        .Received(1)
        .Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => _.EventTypes.SequenceEqual(new[] { EventType.Unknown })));
}
