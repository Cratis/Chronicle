// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer;

/// <summary>
/// Clearing the quarantine of an observer nobody is subscribed to runs the same routing pass an unsubscribed observer
/// goes through whenever its grain is activated, so it ends up exactly where such an observer does.
/// </summary>
public class when_clearing_observer_quarantine_after_reactivation_while_replaying : given.a_reactivated_quarantined_observer
{
    Key _catchingUpPartition;
    EventType[] _definedEventTypes;

    void Establish()
    {
        _definedEventTypes = [EventType.Unknown];
        _definitionStorage.State = _definitionStorage.State with { EventTypes = _definedEventTypes };
        _catchingUpPartition = "partition-catching-up";
        _stateStorage.State = _stateStorage.State with
        {
            IsReplaying = true,
            InFlightPartitions = new HashSet<Key> { _catchingUpPartition },
            CatchingUpPartitions = new HashSet<Key> { _catchingUpPartition }
        };
    }

    async Task Because() => await _observer.ClearObserverQuarantine();

    [Fact] async Task should_not_be_quarantined() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] async Task should_be_disconnected_as_any_reactivation_of_an_unsubscribed_observer_is() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_preserve_the_defined_event_types() => _definitionStorage.State.EventTypes.ShouldEqual(_definedEventTypes);
    [Fact] void should_not_start_a_replay_job() => _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] void should_still_be_marked_as_replaying() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_forget_partitions_catching_up_as_any_reactivation_of_an_unsubscribed_observer_does() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
}
