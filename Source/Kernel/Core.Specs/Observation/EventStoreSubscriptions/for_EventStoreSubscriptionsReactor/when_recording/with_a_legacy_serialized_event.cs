// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage;
using Cratis.Json;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptionsReactor.when_recording;

public class with_a_legacy_serialized_event : Specification
{
    const string LegacyContent = """{"sourceEventStore":"source","eventTypes":[{"id":"customer","generation":1}],"generationDelivery":0,"namedTags":[]}""";
    const string RestrictedContent = """{"sourceEventStore":"source","eventTypes":[],"sourceNamespaces":["customers"]}""";

    IEventStoreSubscriptionsManager _manager;
    EventStoreSubscriptionsReactor _reactor;
    EventStoreSubscriptionAdded _legacy;
    EventStoreSubscriptionAdded _restricted;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        _manager = Substitute.For<IEventStoreSubscriptionsManager>();
        factory.GetGrain<IEventStoreSubscriptionsManager>("target", null).Returns(_manager);
        _reactor = new(factory, Substitute.For<IStorage>(), Substitute.For<ILogger<EventStoreSubscriptionsReactor>>());
        var options = new JsonSerializerOptions(Globals.JsonSerializerOptions);
        _legacy = JsonSerializer.Deserialize<EventStoreSubscriptionAdded>(LegacyContent, options)!;
        _restricted = JsonSerializer.Deserialize<EventStoreSubscriptionAdded>(RestrictedContent, options)!;
    }

    async Task Because()
    {
        await _reactor.Added(_legacy, EventContext.Empty with { EventStore = "target", EventSourceId = "subscription" });
        await _reactor.Added(_restricted, EventContext.Empty with { EventStore = "target", EventSourceId = "subscription" });
    }

    [Fact] void should_replay_both_events_ignoring_the_retired_source_namespaces() => _manager.Received(2).Add(Arg.Any<EventStoreSubscriptionDefinition>());
}
