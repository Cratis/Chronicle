// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_legacy_definition_identifier : given.a_pinned_subscriber
{
    void Establish() => _definition = _definition with { Identifier = "subscription->Lobby" };

    async Task Because() => _result = await Forward();

    [Fact] void should_use_the_supported_exact_legacy_identifier_match() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_apply_the_linked_delivery_policy() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("released");
    [Fact] void should_acknowledge_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
