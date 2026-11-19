// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_source_generation_newer_than_the_target : given.a_pinned_subscriber
{
    void Establish() => _definition = _definition with { GenerationDelivery = EventGenerationDelivery.Compatibility, EventTypes = [] };
    async Task Because() => _result = await Forward();
    [Fact] void should_forward_the_highest_registered_target_generation() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_release_the_migrated_content() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("released");
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_acknowledge_the_source_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
}
