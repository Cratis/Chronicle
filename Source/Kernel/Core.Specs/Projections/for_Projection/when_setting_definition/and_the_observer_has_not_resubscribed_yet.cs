// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

/// <summary>
/// The manager re-subscribes the observer only after SetDefinition returned, and the replay job reads the event types
/// of that subscription. A replay that fired first would run over the previous definition's event types (#5071).
/// </summary>
public class and_the_observer_has_not_resubscribed_yet : given.a_projection_adding_historical_event_mappings
{
    void Establish() => SetObserverEventTypes([_existingEventType]);

    async Task Because()
    {
        await _grain.SetDefinition(_candidate);
        await _silo.TimerRegistry.FireAllAsync();
        _replayedBeforeResubscribing = _observer.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(Observation.IObserver.Replay));
        _timersWhileWaiting = _silo.TimerRegistry.NumberOfActiveTimers;

        SetObserverEventTypes([_existingEventType, _addedEventType]);
        await _silo.TimerRegistry.FireAllAsync();
    }

    bool _replayedBeforeResubscribing;
    int _timersWhileWaiting;

    [Fact] void should_not_replay_before_the_observer_resubscribed() => _replayedBeforeResubscribing.ShouldBeFalse();
    [Fact] void should_keep_waiting_for_the_subscription() => _timersWhileWaiting.ShouldEqual(1);
    [Fact] async Task should_replay_once_the_observer_has_the_new_event_types() => await _observer.Received(1).Replay();
}
