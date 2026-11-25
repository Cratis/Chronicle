// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_no_downward_migration_to_the_target_generation : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        var schema = await _storage.GetEventStore("Lobby").EventTypes.GetFor("event-type-1", 1);
        _storage.GetEventStore("Admin").EventTypes.HasFor("event-type-1", 1).Returns(true);
        _storage.GetEventStore("Admin").EventTypes.GetDefinition("event-type-1").Returns(new EventTypeDefinition("event-type-1", EventTypeOwner.Client, false, [new(1, schema.Schema)], []));
        _storage.GetEventStore("Admin").EventTypes.GetFor("event-type-1", 1).Returns(schema);
        _events = [_events[0] with { Context = _events[0].Context with { EventType = new("event-type-1", 2) } }];
        _generationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>()).Returns(Task.FromException<AppendedEvent[]>(new PinnedEventTypeGenerationUnavailable(new("event-type-1", 1))));
    }

    async Task Because() => _result = await Forward();
    [Fact] void should_fail() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_park_the_partition() => _result.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.ParkPartition);
    [Fact] void should_not_append() => AppendCalls().ShouldEqual(0);
    [Fact] void should_name_the_event_type() => _result.ExceptionMessages.Single().ShouldContain("event-type-1");
    [Fact] void should_name_the_source_generation() => _result.ExceptionMessages.Single().ShouldContain("generation 2");
    [Fact] void should_name_the_target_generation() => _result.ExceptionMessages.Single().ShouldContain("target registers generation(s) 1");
}
