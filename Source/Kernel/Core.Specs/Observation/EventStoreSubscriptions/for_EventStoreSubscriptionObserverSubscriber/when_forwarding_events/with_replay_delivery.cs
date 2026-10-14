// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_replay_delivery : given.a_pinned_subscriber
{
    void Establish() => _events = [_events[0] with { Context = _events[0].Context with { ObservationState = EventObservationState.Replay } }];
    async Task Because() => _result = await Forward();
    [Fact] void should_forward_the_same_pin_on_replay() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_release_the_same_content_on_replay() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("released");
    [Fact] void should_acknowledge_success() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
