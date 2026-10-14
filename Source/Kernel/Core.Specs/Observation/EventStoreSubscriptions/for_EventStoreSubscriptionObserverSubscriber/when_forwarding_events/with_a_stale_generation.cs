// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_stale_generation : given.a_pinned_subscriber
{
    async Task Because() => _result = await Forward();
    [Fact] void should_forward_the_target_pin() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_select_and_release_that_generations_content() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("released");
    [Fact] void should_succeed_with_a_target_knowing_only_generation_one() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_acknowledge_the_source_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);
}
