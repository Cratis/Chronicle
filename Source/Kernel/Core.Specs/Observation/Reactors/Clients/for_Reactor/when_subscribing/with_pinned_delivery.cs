// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class with_pinned_delivery : given.a_reactor_grain_with_replay_on_definition_change
{
    IObserver _observer;
    void Establish()
    {
        _observer = _observerProbe;
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Pinned };
    }
    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);
    [Fact] async Task should_pass_the_delivery_policy() => await _observer.Received(1).SubscribeWithGenerationDelivery<IReactorObserverSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
}
