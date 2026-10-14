// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_compatibility_to_pinned_transition : given.a_pinned_subscriber
{
    async Task Establish()
    {
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Compatibility };
        await Forward();
        _inboxSequence.ClearReceivedCalls();
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Pinned };
    }
    async Task Because() => _result = await Forward();
    [Fact] void should_resolve_the_current_pin_even_with_legacy_string_metadata() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_release_the_current_generations_content() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("released");
}
