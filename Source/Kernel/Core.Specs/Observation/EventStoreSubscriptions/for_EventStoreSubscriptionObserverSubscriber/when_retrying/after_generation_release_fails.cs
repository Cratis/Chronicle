// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_retrying;

public class after_generation_release_fails : given.a_pinned_subscriber
{
    ObserverSubscriberResult _first;
    void Establish() => _metadata.Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<JsonObject>()).Returns(Task.FromException<JsonObject>(new IOException("Release unavailable")), Task.FromResult(new JsonObject { ["message"] = "released" }));
    async Task Because()
    {
        _first = await Forward();
        _result = await Forward();
    }
    [Fact] void should_not_advance_on_release_failure() => _first.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_append_before_release_succeeds() => AppendCalls().ShouldEqual(1);
    [Fact] void should_succeed_after_release_recovers() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_deliver_the_current_pin() => ForwardedType().ShouldEqual(new EventType("event-type-1", 1));
}
