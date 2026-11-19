// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_target_without_the_event_type : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result;

    void Establish()
    {
        _events = [_events[0]];
        var types = _storage.GetEventStore("Lobby").EventTypes;
        types.HasFor("event-type-1").Returns(false);
        types.HasFor("event-type-1", 1).Returns(false);
    }

    async Task Because() => _result = await Forward();
    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_append_once() => AppendCalls().ShouldEqual(1);
    [Fact] void should_forward_the_type_unchanged() => ((EventType)AppendArguments()[4]!).ShouldEqual(_events[0].Context.EventType);
    [Fact] void should_forward_the_content_unchanged() => ((JsonObject)AppendArguments()[5]!)["message"]!.GetValue<string>().ShouldEqual("private-content-marker");
    [Fact] void should_not_release_again() => _generationRelease.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_acknowledge_the_source_position() => _result.LastSuccessfulObservation.ShouldEqual((EventSequenceNumber)42UL);

    object?[] AppendArguments() => _inboxSequence.ReceivedCalls().Single(call => call.GetMethodInfo().Name == "Append").GetArguments();
}
