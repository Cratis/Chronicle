// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class without_materialized_pinned_content : given.a_pinned_subscriber
{
    void Establish() => _events = [_events[0] with { GenerationalContent = new Dictionary<int, string> { [2] = "{\"message\":\"new-generation\"}" } }];
    async Task Because() => _result = await Forward();
    [Fact] void should_migrate_for_delivery() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("new-generation");
    [Fact] void should_append_the_requested_generation() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}
