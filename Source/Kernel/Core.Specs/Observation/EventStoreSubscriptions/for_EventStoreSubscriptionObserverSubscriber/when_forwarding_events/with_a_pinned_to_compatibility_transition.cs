// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_pinned_to_compatibility_transition : given.a_pinned_subscriber
{
    async Task Establish()
    {
        await Forward();
        _inboxSequence.ClearReceivedCalls();
        _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Compatibility };
    }
    async Task Because() => _result = await Forward();
    [Fact] void should_stop_releasing_into_the_old_pin() => ForwardedType().ShouldEqual(new EventType("event-type-1", 2));
    [Fact] void should_forward_incoming_content_unchanged() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("private-content-marker");
}
