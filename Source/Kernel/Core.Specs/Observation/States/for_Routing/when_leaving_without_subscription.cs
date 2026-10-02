// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.States.for_Routing;

public class when_leaving_without_subscription : given.a_routing_state
{
    EventType[] _definedEventTypes;

    async Task Establish()
    {
        _definedEventTypes = [EventType.Unknown];
        _definitionState.State = _definitionState.State with { EventTypes = _definedEventTypes };
        _subscription = ObserverSubscription.Unsubscribed;
        _storedState = await _state.OnEnter(_storedState);
    }

    Task Because() => _state.OnLeave(_storedState);

    [Fact] void should_preserve_the_defined_event_types() => _definitionState.State.EventTypes.ShouldEqual(_definedEventTypes);
}
