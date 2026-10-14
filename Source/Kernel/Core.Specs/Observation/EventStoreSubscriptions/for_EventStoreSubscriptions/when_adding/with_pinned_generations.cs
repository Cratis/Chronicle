// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_pinned_generations : given.a_pinned_registration
{
    async Task Because() => await _service.Add(_request);
    [Fact] void should_reserve_the_exact_pin_and_mode() => _manager.Received(1).Add(Arg.Is<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>(definition => definition.GenerationDelivery == EventGenerationDelivery.Pinned && definition.EventTypes.Single() == _pin));
    [Fact] void should_persist_the_pin_in_the_registration_event() => AddedEvent().EventTypes.ShouldContainOnly(_pin);
    [Fact] void should_persist_the_delivery_policy_in_the_registration_event() => AddedEvent().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
