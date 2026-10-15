// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class without_start_at_tail : given.a_reactor_grain_with_replay_on_definition_change
{
    IObserver _observer;

    void Establish() => _observer = _observerProbe;

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);

    [Fact] async Task should_leave_the_position_alone() => await _observer.DidNotReceive().StartAtTailWhenNew();
}
