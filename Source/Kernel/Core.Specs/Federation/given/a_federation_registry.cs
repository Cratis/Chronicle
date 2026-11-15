// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A federation registry over an in-memory fake of the system event log.
/// </summary>
public class a_federation_registry : Specification
{
    protected FederationRegistry _registry;
    protected ChronicleOptions _options;
    protected IEventSequence _eventLog;
    protected List<(string Id, object Content)> _appended;
    protected List<AppendedEvent> _stored;
    protected Dictionary<ulong, object> _contents;
    protected string _pin;
    protected string _otherPin;
    IEventSequenceStorage _events;
    IEventSerializer _serializer;
    IGrainFactory _grainFactory;
    IStorage _storage;

    void Establish()
    {
        _pin = Pin(1);
        _otherPin = Pin(2);
        _appended = [];
        _stored = [];
        _contents = [];
        _options = new();
        _events = Substitute.For<IEventSequenceStorage>();
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(EventStoreName.System).GetNamespace(EventStoreNamespaceName.Default).GetEventSequence(EventSequenceId.Log).Returns(_events);
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventLog = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventLog);
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Deserialize(Arg.Any<AppendedEvent>()).Returns(call => _contents[call.Arg<AppendedEvent>().Context.SequenceNumber.Value]);

        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<Causation>?>(),
            Arg.Any<Identity?>(),
            Arg.Any<IEnumerable<Tag>?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>())
            .Returns(call => Task.FromResult(Store(call.Arg<EventSourceId>(), call.Arg<object>())));

        _events.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>())
            .Returns(call => Task.FromResult(Matching(call.ArgAt<IEnumerable<EventType>>(0)).LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable));
        _events.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(Cursor(Matching(call.ArgAt<IEnumerable<EventType>>(3))
                .Where(_ => _.Context.SequenceNumber >= call.ArgAt<EventSequenceNumber>(0) && _.Context.SequenceNumber <= call.ArgAt<EventSequenceNumber>(1)).ToArray())));

        _registry = CreateRegistry();
    }

    protected static string Pin(byte seed) => new CertificatePin(SHA256.HashData([seed])).ToString();

    protected FederationRegistry CreateRegistry() => new(_storage, _grainFactory, _serializer, Options.Create(_options), NullLogger<FederationRegistry>.Instance);

    protected FederationBindingState Binding(string id = "orders", string endpoint = "https://peer.example.com:35100") =>
        new(id, "orders-service", endpoint, _pin, "client-cert", "orders", "Default", "local-orders", "Default", FederationOrigin.Runtime);

    protected FederationGrantState Grant(string id = "link-1", bool allPublic = true, IReadOnlyList<string>? eventTypes = null, IReadOnlyList<string>? namespaces = null) =>
        new(id, _otherPin, "orders", namespaces ?? ["Default"], allPublic, eventTypes ?? [], false, FederationOrigin.Runtime);

    protected void Seed(string id, object content) => Store(id, content, false);

    AppendResult Store(EventSourceId id, object content, bool recordAppend = true)
    {
        var sequenceNumber = (EventSequenceNumber)(ulong)_stored.Count;
        var context = EventContext.From(
            EventStoreName.System,
            EventStoreNamespaceName.Default,
            content.GetType().GetEventType(),
            EventSourceType.Default,
            id,
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.New());
        var stored = new AppendedEvent(context, new());
        _stored.Add(stored);
        _contents[sequenceNumber.Value] = content;
        if (recordAppend)
        {
            _appended.Add((id.Value, content));
        }

        return AppendResult.Success(CorrelationId.New(), sequenceNumber);
    }

    AppendedEvent[] Matching(IEnumerable<EventType>? types) =>
        [.. _stored.Where(_ => types?.Contains(_.Context.EventType) != false)];

    static IEventCursor Cursor(AppendedEvent[] events)
    {
        var cursor = Substitute.For<IEventCursor>();
        cursor.Current.Returns(events);
        cursor.MoveNext().Returns(Task.FromResult(true), Task.FromResult(false));
        return cursor;
    }
}
