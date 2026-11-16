// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionObserverSubscriber.when_forwarding_events;

public class with_a_target_schema_differing_in_casing : given.a_subscriber_with_events
{
    ObserverSubscriberResult _result = null!;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"Message":{"type":"string"}}}""");
        var types = _storage.GetEventStore("Lobby").EventTypes;
        types.HasFor("event-type-1", 1).Returns(true);
        types.GetFor("event-type-1", 1).Returns(new EventTypeSchema(new("event-type-1", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
    }

    async Task Because() => _result = await Forward();

    [Fact] void should_succeed() => _result.State.ShouldEqual(ObserverSubscriberState.Ok);
    [Fact] void should_forward_the_property_with_the_target_casing() => ((JsonObject)_inboxSequence.ReceivedCalls().First(call => call.GetMethodInfo().Name == "Append").GetArguments()[5]!).ContainsKey("Message").ShouldBeTrue();
}
