// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_source_generation_the_target_also_registers : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    async Task Establish()
    {
        var types = _storage.GetEventStore("Lobby").EventTypes;
        var definition = await types.GetDefinition("event-type-1");
        var schema = definition.Generations.Single().Schema;
        types.GetDefinition("event-type-1").Returns(definition with { Generations = [new(2, schema)] });
        types.HasFor("event-type-1", 2).Returns(true);
        types.GetFor("event-type-1", 2).Returns(new Concepts.EventTypes.EventTypeSchema(new("event-type-1", 2), EventTypeOwner.Client, EventTypeSource.Code, schema));
        _events = [_events[0] with { Context = _events[0].Context with { EventType = new("event-type-1", 2) } }];
    }

    async Task Because() => _result = await Forward();
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_forward_the_source_generation() => ((EventType)_inboxSequence.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Append").GetArguments()[4]!).Generation.ShouldEqual((EventTypeGeneration)2);
    [Fact] void should_not_release_again() => _generationRelease.ReceivedCalls().ShouldBeEmpty();
}
