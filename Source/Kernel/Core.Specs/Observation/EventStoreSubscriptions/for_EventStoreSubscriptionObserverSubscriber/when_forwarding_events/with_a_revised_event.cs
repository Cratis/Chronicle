// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_revised_event : given.a_pinned_subscriber
{
    void Establish() => _events = [_events[0] with { RevisedGeneration = 2 }];
    async Task Because() => _result = await Forward();
    [Fact] void should_migrate_the_revision_instead_of_selecting_old_materialization() => ForwardedContent()["message"]!.GetValue<string>().ShouldEqual("private-content-marker");
    [Fact] void should_forward_the_pin() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}
