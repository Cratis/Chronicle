// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_only_a_non_default_target_generation : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        var target = _storage.GetEventStore("Lobby").EventTypes;
        var schema = (await target.GetFor("event-type-1", 1)).Schema;
        var type = new EventType("event-type-1", 2);
        var definition = new EventTypeDefinition(type.Id, EventTypeOwner.Client, false, [new(2, schema)], []);
        var targetSchema = new EventTypeSchema(type, EventTypeOwner.Client, EventTypeSource.Code, schema);
        target.GetDefinition(type.Id).Returns(definition);
        target.HasFor(type.Id).Returns(false);
        target.HasFor(type.Id, 1).Returns(false);
        target.HasFor(type.Id, 2).Returns(true);
        target.GetFor(type.Id, 2).Returns(targetSchema);
        target.GetAllGenerationsForEventType(Arg.Is<EventType>(_ => _.Id == type.Id)).Returns([targetSchema]);
        var source = _storage.GetEventStore("Admin").EventTypes;
        source.GetDefinition(type.Id).Returns(definition);
        source.HasFor(type.Id, 2).Returns(true);
        source.GetFor(type.Id, 2).Returns(targetSchema);
        _events = _events.Take(2).Select(@event => @event with { Context = @event.Context with { EventType = new(type.Id, 3) } }).ToArray();
        var stored = _events.ToArray();
        _storage.GetEventStore("Admin").GetNamespace(EventStoreNamespaceName.Default).GetEventSequence("outbox")
            .GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => stored.Single(@event => @event.Context.SequenceNumber == call.Arg<EventSequenceNumber>()));
        _generationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => call.ArgAt<IEnumerable<AppendedEvent>>(3).Select(@event => @event with { Context = @event.Context with { EventType = type }, Content = ReleasedContent() }).ToArray());
    }

    static ExpandoObject ReleasedContent()
    {
        dynamic content = new ExpandoObject();
        content.message = "downcast-to-2";
        return content;
    }

    async Task Because() => _result = await Forward();
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_forward_both_events() => AppendCalls().ShouldEqual(2);
    [Fact] void should_forward_generation_two() => _inboxSequence.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Append").Select(call => ((EventType)call.GetArguments()[4]!).Generation).All(_ => _ == (EventTypeGeneration)2).ShouldBeTrue();
    [Fact] void should_release_the_content() => _inboxSequence.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Append").Select(call => ((JsonObject)call.GetArguments()[5]!)["message"]!.GetValue<string>()).All(_ => _ == "downcast-to-2").ShouldBeTrue();
    [Fact] void should_query_target_generations_once_per_page() => _storage.GetEventStore("Lobby").EventTypes.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "GetAllGenerationsForEventType").ShouldEqual(1);
}
