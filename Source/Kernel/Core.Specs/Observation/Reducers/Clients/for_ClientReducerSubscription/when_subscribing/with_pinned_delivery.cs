// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_subscribing;

public class with_pinned_delivery : given.a_client_reducer_subscription_with_replay_on_definition_change
{
    void Establish() => _definition = _definition with { IsActive = true, GenerationDelivery = EventGenerationDelivery.Pinned };
    async Task Because() => await _subscription.SetDefinitionAndSubscribe(_definition);
    [Fact] async Task should_pass_the_delivery_policy() => await _observer.Received(1).SubscribeWithGenerationDelivery<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<EventTypeSet>(), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());
}
