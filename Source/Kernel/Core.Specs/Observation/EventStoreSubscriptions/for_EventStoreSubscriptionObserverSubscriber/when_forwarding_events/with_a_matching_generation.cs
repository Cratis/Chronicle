// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_matching_generation : given.a_pinned_subscriber
{
    void Establish() => _events = [_events[0] with { Context = _events[0].Context with { EventType = new("event-type-1", 1) } }];
    async Task Because() => _result = await Forward();
    [Fact] void should_forward_without_releasing_twice() => _metadata.DidNotReceive().Release(Arg.Any<Concepts.EventStoreName>(), Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<System.Text.Json.Nodes.JsonObject>());
    [Fact] void should_forward_the_matching_generation() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
}
