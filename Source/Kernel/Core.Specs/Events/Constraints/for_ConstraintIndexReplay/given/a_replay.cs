// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.given;

public class a_replay : Specification
{
    protected readonly List<AppendedEvent> _events = [];
    protected readonly List<string> _writes = [];
    protected readonly List<int> _readsAtWrites = [];
    protected IEventSequenceStorage _sequence;
    protected IUniqueConstraintsStorage _unique;
    protected Storage.InMemory.Events.Constraints.UniqueConstraintsStorage _claims;
    protected Storage.InMemory.Events.Constraints.ClosedStreamsConstraintStorage _closed;
    protected IClosedStreamsConstraintStorage _closureStorage;
    protected UniqueConstraintDefinition _definition;
    protected ClosesStreamConstraintDefinition _closing;
    protected ConstraintIndexReplay _replay;
    protected ConstraintIndexReplayResult _result;
    protected StoredConstraintEventContent _content;
    protected IEventTypesStorage _types;
    protected IJsonSchemaMetadataManager _metadata;
    protected IExpandoObjectConverter _converter;

    void Establish()
    {
        _claims = new();
        _closed = new();
        _closureStorage = _closed;
        _definition = new("unique", [new("Claimed", ["email"])], ["Removed"]);
        _closing = new("closing", ["Closed"], ClosedStreamDimensions.EventSourceId, ["Reopened"]);
        _sequence = Substitute.For<IEventSequenceStorage>();
        _sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => new EventCursor(_events.Where(@event => @event.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && call.ArgAt<IEnumerable<EventType>>(5).Any(type => type.Id == @event.Context.EventType.Id)).ToList()));
        _unique = Substitute.For<IUniqueConstraintsStorage>();
        _unique.GetFor(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.GetFor(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2)));
        _unique.IsAllowed(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.IsAllowed(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<UniqueConstraintValue>(2), call.ArgAt<string>(3)));
        _unique.GetForStream(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _claims.GetForStream(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2), call.ArgAt<CancellationToken>(3)));
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<Subject?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _writes.Add($"claim:{call.ArgAt<EventSourceId>(0)}:{call.ArgAt<UniqueConstraintValue>(3)}");
                _readsAtWrites.Add(_sequence.ReceivedCalls().Count());
                return _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4));
            });
        _unique.Remove(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _writes.Add($"release:{call.ArgAt<EventSourceId>(0)}");
                return _claims.Remove(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<string>(2));
            });
        _unique.RemoveValue(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _writes.Add($"release:{call.ArgAt<EventSourceId>(0)}:{call.ArgAt<UniqueConstraintValue>(2)}");
                return _claims.RemoveValue(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<UniqueConstraintValue>(2), call.ArgAt<string>(3));
            });
        _types = Substitute.For<IEventTypesStorage>();
        _types.GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration?>())
            .Returns(call => new EventTypeSchema(new(call.ArgAt<EventTypeId>(0), call.ArgAt<EventTypeGeneration>(1)), EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));
        _metadata = Substitute.For<IJsonSchemaMetadataManager>();
        _converter = Substitute.For<IExpandoObjectConverter>();
        _content = new(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, _types, _metadata, _converter);
    }

    protected void Start(int cap = 100000, EventSequenceNumber? closingStart = null, bool noConstraints = false) =>
        _replay = new(
            _sequence,
            _unique,
            _closureStorage,
            _content,
            Substitute.For<ILogger<ConstraintIndexReplay>>(),
            noConstraints ? [] : [new(_definition, EventSequenceNumber.First), new(_closing, closingStart ?? EventSequenceNumber.First)],
            _events.Count == 0 ? EventSequenceNumber.First : _events.Max(@event => @event.Context.SequenceNumber).Next(),
            cap);

    protected void Event(ulong number, string source, string type, string? value = null, uint generation = 1)
    {
        var content = new ExpandoObject();
        if (value is not null) ((IDictionary<string, object?>)content)["email"] = value;
        _events.Add(new(EventContext.From(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, new(type, generation), EventSourceType.Default, source, EventStreamType.All, EventStreamId.Default, number, CorrelationId.NotSet), content));
    }

    protected static UniqueConstraintValue Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    protected async Task<string[]> Values(string source) => (await _claims.GetFor(source, _definition)).Select(value => value.Value).ToArray();
}
