// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_only_a_newer_generation_registered_at_the_target : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        _events = [_events[0]];
        var types = _storage.GetEventStore("Lobby").EventTypes;
        var definition = await types.GetDefinition("event-type-1");
        types.GetDefinition("event-type-1").Returns(definition with { Generations = [new(2, definition.Generations.Single().Schema)] });
        types.HasFor("event-type-1", 1).Returns(false);
        types.GetAllGenerationsForEventType(Arg.Is<EventType>(_ => _.Id == (EventTypeId)"event-type-1")).Returns([new EventTypeSchema(new("event-type-1", 2), EventTypeOwner.Client, EventTypeSource.Code, definition.Generations.Single().Schema)]);
    }

    async Task Because() => _result = await Forward();
    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_park_the_partition() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.ParkPartition);
    [Fact] void should_not_append() => AppendCalls().ShouldEqual(0);
    [Fact] void should_name_the_source_generation() => _result.ExceptionMessages.Single().ShouldContain("generation 1");
    [Fact] void should_name_the_target_generation() => _result.ExceptionMessages.Single().ShouldContain("target registers generation(s) 2");
}
