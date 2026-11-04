// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_repeated_replay_delivery : given.a_subscriber_with_events
{
    void Establish() => _events = [_events[0] with { Context = _events[0].Context with { ObservationState = EventObservationState.Replay } }];

    async Task Because()
    {
        (await Forward()).State.ShouldEqual(ObserverSubscriberState.Ok);
        (await Forward()).State.ShouldEqual(ObserverSubscriberState.Ok);
    }

    [Fact] void should_append_twice_rather_than_deduplicate() => AppendCalls().ShouldEqual(2);

    [Fact]
    void should_preserve_the_event_source_on_both_appends()
    {
        foreach (var call in _inboxSequence.ReceivedCalls())
        {
            call.GetArguments()[1].ShouldEqual(_events[0].Context.EventSourceId);
        }
    }

    [Fact]
    void should_preserve_the_payload_on_both_appends()
    {
        foreach (var call in _inboxSequence.ReceivedCalls())
        {
            ((JsonObject)call.GetArguments()[5]!).ToJsonString().ShouldEqual("{\"message\":\"private-content-marker\"}");
        }
    }
}
