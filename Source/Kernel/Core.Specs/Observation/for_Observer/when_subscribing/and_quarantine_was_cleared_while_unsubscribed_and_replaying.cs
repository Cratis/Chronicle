// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_was_cleared_while_unsubscribed_and_replaying : given.a_reactivated_quarantined_observer
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { EventTypes = [new EventType("original-event", EventTypeGeneration.First)] };
        await _definitionStorage.WriteStateAsync();
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _stateStorage.WriteStateAsync();
        await _observer.ClearObserverQuarantine();
    }

    Task Because() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero);

    [Fact] async Task should_be_replaying() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Replay>();
    [Fact] void should_keep_the_replay_flag() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_start_one_replay_for_the_subscribed_event_types() => _jobsManager.Received(1).Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => _.EventTypes.SequenceEqual(new[] { EventType.Unknown })));
    [Fact] void should_not_start_a_replay_with_empty_event_types() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Is<ReplayObserverRequest>(_ => !_.EventTypes.Any()));
}
