// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_subscribed_to_all_events_with_no_fixed_event_types : given.a_routing_state
{
    void Establish()
    {
        // Mirrors Observer.SubscribeToAllEvents: no fixed event type list on the subscription, by design,
        // since the whole point is to also cover event types that do not exist yet.
        _subscription = new ObserverSubscription(
            _observerKey.ObserverId,
            _observerKey,
            [],
            typeof(object),
            SiloAddress.Zero,
            string.Empty);
        _observer.GetSubscription().Returns(_ => _subscription);

        _storedState = _storedState with { SubscribesToAllEvents = true };
    }

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_disconnect() => _stateMachine.DidNotReceive().TransitionTo<Disconnected>();
}
