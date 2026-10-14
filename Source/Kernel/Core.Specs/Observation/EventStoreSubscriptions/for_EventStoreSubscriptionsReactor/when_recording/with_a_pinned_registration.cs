// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsReactor.when_recording;

public class with_a_pinned_registration : Specification
{
    EventStoreSubscriptionsReactor _reactor;
    IEventStoreSubscriptionsManager _manager;
    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IEventStoreSubscriptionsManager>();
        factory.GetGrain<IEventStoreSubscriptionsManager>("target", null).Returns(_manager);
        _reactor = new(factory, Substitute.For<IStorage>(), Substitute.For<ILogger<EventStoreSubscriptionsReactor>>());
    }
    async Task Because() => await _reactor.Added(new EventStoreSubscriptionAdded("source", [new EventType("customer", 3)]) { GenerationDelivery = EventGenerationDelivery.Pinned }, EventContext.Empty with { EventStore = "target", EventSourceId = "subscription" });
    [Fact] void should_preserve_the_mode_and_exact_pin_in_manager_state() => _manager.Received(1).Add(Arg.Is<EventStoreSubscriptionDefinition>(definition => definition.GenerationDelivery == EventGenerationDelivery.Pinned && definition.EventTypes.Single() == new EventType("customer", 3)));
}
