// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.for_EventSequencesReactor.given;

public class a_reactor : Specification
{
    protected static readonly EventType EventType = new("some-event", 1);

    protected EventSequencesReactor _reactor;
    protected IGrainFactory _grainFactory;
    protected IEventSequence _sequence;
    protected IStorage _storage;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _sequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>(), Arg.Any<string?>()).Returns(_sequence);
        _storage = Substitute.For<IStorage>();

        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        var types = Substitute.For<IEventTypesStorage>();
        var sequenceStorage = Substitute.For<IEventSequenceStorage>();
        var schema = JsonSchema.FromJsonAsync("""{"type":"object"}""").GetAwaiter().GetResult();
        _storage.GetEventStore(Arg.Any<Concepts.EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<Concepts.EventStoreNamespaceName>()).Returns(ns);
        store.EventTypes.Returns(types);
        ns.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(sequenceStorage);
        types.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(new EventTypeSchema(EventType, EventTypeOwner.Server, EventTypeSource.Code, schema));
        sequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => new AppendedEvent(ContextFor("user-store"), new ExpandoObject()));

        var metadataManager = Substitute.For<IJsonSchemaMetadataManager>();
        metadataManager.ReleaseStrict(default!, default!, default!, default!, default!).ReturnsForAnyArgs(call => call.Arg<JsonObject>());
        _reactor = new EventSequencesReactor(_grainFactory, new JsonSerializerOptions(), NullLogger<EventSequencesReactor>.Instance, _storage, metadataManager);
    }

    protected static EventContext ContextFor(string eventStore) =>
        EventContext.From(eventStore, "some-namespace", EventType, EventSourceType.Default, "some-source", EventStreamType.All, EventStreamId.Default, 1UL, CorrelationId.NotSet);
}
