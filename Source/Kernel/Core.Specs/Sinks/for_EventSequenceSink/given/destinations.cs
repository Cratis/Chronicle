// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.given;

/// <summary>
/// Emulates the destination sequences of every store/namespace/sequence, including the idempotent publication contract.
/// </summary>
public class destinations
{
    readonly Dictionary<string, List<AppendedEvent>> _events = [];
    readonly Dictionary<string, Dictionary<string, (string Fingerprint, EventSequenceNumber Number)>> _publications = [];
    readonly ExpandoObjectConverter _converter = new(new TypeFormats());
    readonly JsonSchema _schema;

    public destinations()
    {
        _schema = JsonSchema.FromJson("{\"type\":\"object\",\"properties\":{\"Total\":{\"type\":\"integer\"},\"Count\":{\"type\":\"integer\"}}}");
        Grains = Substitute.For<IGrainFactory>();
        Grains.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(call => SequenceFor(call.ArgAt<string>(0)));
        Storage = Substitute.For<IStorage>();
        Storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(call =>
        {
            var store = call.Arg<EventStoreName>();
            var eventStore = Substitute.For<IEventStoreStorage>();
            eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(ns =>
            {
                var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
                namespaceStorage.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(seq => StorageFor(Key(store, ns.Arg<EventStoreNamespaceName>(), seq.Arg<EventSequenceId>())));
                return namespaceStorage;
            });
            return eventStore;
        });
    }

    public IGrainFactory Grains { get; }

    public IStorage Storage { get; }

    public IExpandoObjectConverter Converter => _converter;

    public int AppendAttempts { get; private set; }

    public IReadOnlyList<AppendedEvent> Events(string store, string @namespace, EventSequenceId sequence) =>
        _events.TryGetValue(Key(store, @namespace, sequence), out var list) ? list : [];

    public bool Fail { get; set; }

    static string Key(EventStoreName store, EventStoreNamespaceName ns, EventSequenceId sequence) => $"{sequence}+{store}+{ns}";

    IEventSequence SequenceFor(string grainKey)
    {
        var parsed = EventSequenceKey.Parse(grainKey);
        var key = Key(parsed.EventStore, parsed.Namespace, parsed.EventSequenceId);
        var sequence = Substitute.For<IEventSequence>();
        sequence.AppendPublication(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<EventToAppend>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<Identity>())
            .Returns(call =>
            {
                AppendAttempts++;
                var correlation = call.Arg<CorrelationId>();
                if (Fail) return Task.FromResult(AppendResult.Failed(correlation, [new AppendError("refused")]));

                var id = call.ArgAt<string>(0);
                var fingerprint = call.ArgAt<string>(1);
                var @event = call.Arg<EventToAppend>();
                var ids = _publications.TryGetValue(key, out var existingIds) ? existingIds : _publications[key] = [];
                if (ids.TryGetValue(id, out var found))
                {
                    if (found.Fingerprint != fingerprint) throw new EventPublicationConflict();
                    return Task.FromResult(AppendResult.Success(correlation, found.Number));
                }

                var list = _events.TryGetValue(key, out var existing) ? existing : _events[key] = [];
                var number = new EventSequenceNumber((ulong)list.Count);
                var context = EventContext.From(parsed.EventStore, parsed.Namespace, @event.EventType, @event.EventSourceType, @event.EventSourceId, @event.eventStreamType, @event.eventStreamId, number, correlation, null, @event.Occurred, @event.Subject) with
                {
                    Causation = call.Arg<IEnumerable<Causation>>(),
                    CausedBy = call.Arg<Identity>()
                };
                list.Add(new AppendedEvent(context, _converter.ToExpandoObject(@event.Content, _schema)));
                ids[id] = (fingerprint, number);
                return Task.FromResult(AppendResult.Success(correlation, number));
            });
        return sequence;
    }

    IEventSequenceStorage StorageFor(string key)
    {
        var storage = Substitute.For<IEventSequenceStorage>();
        storage.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>()).Returns(call =>
        {
            var source = call.Arg<EventSourceId>();
            var types = call.Arg<IEnumerable<EventTypeId>>().ToHashSet();
            var found = (_events.TryGetValue(key, out var list) ? list : []).LastOrDefault(_ => _.Context.EventSourceId == source && types.Contains(_.Context.EventType.Id));
            return Task.FromResult(found is null ? Option<AppendedEvent>.None() : (Option<AppendedEvent>)found);
        });
        storage.TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>()).Returns(call =>
        {
            var type = call.Arg<EventTypeId>();
            var source = call.Arg<EventSourceId>();
            var before = call.Arg<EventSequenceNumber>();
            var found = (_events.TryGetValue(key, out var list) ? list : []).LastOrDefault(_ => _.Context.EventSourceId == source && _.Context.EventType.Id == type && _.Context.SequenceNumber < before);
            return Task.FromResult(Catch<Option<AppendedEvent>>.Success(found is null ? Option<AppendedEvent>.None() : (Option<AppendedEvent>)found));
        });
        return storage;
    }
}
