// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.given;

public class a_backfill : Specification
{
    protected static readonly EventTypeId TypeId = "backfilled-type";
    protected IStorage _storage;
    protected IEventSequenceStorage _sequence;
    protected IEventTypesStorage _eventTypes;
    protected StoredEventGenerations _snapshot;
    protected EventTypeDefinition _definition;
    protected List<GenerationToAdd> _additions = [];
    protected List<StoredEventGenerations> _writes = [];
    protected Func<StoredEventGenerations, bool> _writeResult = _ => true;
    EventTypeGenerationBackfill _backfill;

    void Establish()
    {
        var first = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string"},"details":{"type":"string"}}}""");
        var second = JsonSchema.FromJson("""{"type":"object","properties":{"value":{"type":"string"}}}""");
        var third = JsonSchema.FromJson("""{"type":"object","properties":{"renamed":{"type":"string"}}}""");
        _definition = new(TypeId, EventTypeOwner.Server, false, [new(1, first), new(2, second), new(3, third)], [new(1, 2, [], new JsonObject(), new JsonObject()), new(2, 3, [], new JsonObject { ["renamed"] = "value" }, new JsonObject { ["value"] = "renamed" })]);
        _snapshot = new(0, TypeId, "source", Subject.NotSet, 1, new Dictionary<EventTypeGeneration, string> { [1] = """{"value":"original","details":"keep me"}""", [2] = """{"value":"derived"}""" }, 0, "observed");
        _storage = Substitute.For<IStorage>();
        _sequence = Substitute.For<IEventSequenceStorage>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        var store = _storage.GetEventStore((Concepts.EventStoreName)"store");
        store.EventTypes.Returns(_eventTypes);
        store.GetNamespace((Concepts.EventStoreNamespaceName)"default").GetEventSequence(WellKnownEventSequences.EventLog).Returns(_sequence);
        _eventTypes.GetDefinition(TypeId).Returns(_ => _definition);
        _sequence.GetStoredGenerations(0).Returns(_ => _snapshot);
        _sequence.TryAddGenerations(Arg.Any<StoredEventGenerations>(), Arg.Any<IEnumerable<GenerationToAdd>>()).Returns(call =>
        {
            var observed = call.Arg<StoredEventGenerations>();
            _writes.Add(observed);
            _additions.AddRange(call.Arg<IEnumerable<GenerationToAdd>>());
            return _writeResult(observed);
        });
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([new AppendedEvent(EventContext.From("store", "default", new(TypeId, 2), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, 0, CorrelationId.NotSet), new ExpandoObject())]);
        _sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        _backfill = new(_storage, new EventTypeMigrations(_storage, converter), Substitute.For<IJsonSchemaMetadataManager>(), converter, new EventHashCalculator());
    }

    protected Task Perform() => _backfill.Perform("store", "default", TypeId, NullLogger.Instance, CancellationToken.None);
    protected string AddedValue() => (string)((IDictionary<string, object?>)_additions[0].Content)["renamed"]!;
}
