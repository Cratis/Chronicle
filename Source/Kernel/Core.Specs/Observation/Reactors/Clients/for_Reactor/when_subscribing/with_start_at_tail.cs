// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_start_at_tail : given.a_reactor_grain_with_replay_on_definition_change
{
    IObserver _observer;

    void Establish()
    {
        _observer = _observerProbe;
        _definition = _definition with { StartAtTail = true };
    }

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);

    [Fact] async Task should_position_the_observer_at_the_tail() => await _observer.Received(1).StartAtTailWhenNew();
    [Fact] void should_do_so_before_subscribing() => Received.InOrder(() =>
    {
        _observer.StartAtTailWhenNew();
        _observer.Subscribe<IReactorObserverSubscriber>(Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverType>(), Arg.Any<IEnumerable<Cratis.Chronicle.Concepts.Events.EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>());
    });
}
