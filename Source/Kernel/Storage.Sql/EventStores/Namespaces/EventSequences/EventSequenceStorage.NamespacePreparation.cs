// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    static EventEntry NamespaceEntry(EventToAppendToStorage item, bool persistIdentity)
    {
        var entry = EventEntryConverter.ToEventEntry(item.SequenceNumber, item.EventSourceType, item.EventSourceId, item.EventStreamType, item.EventStreamId, item.EventType, item.CorrelationId, item.Causation, item.CausedByChain, item.Tags, TruncateToMicrosecond(item.Occurred), item.GenerationalContent, item.ContentHashes, item.Subject?.IsSet == true ? item.Subject : null);
        entry.EventSource = item.EventSource.IsSet ? item.EventSource.Value : null;
        entry.CompensationFor = item.CompensationFor?.Value;
        if (persistIdentity && item.Identity is { } identity)
        {
            entry.PublicationIdentityHash = PublicationIdentityHash(identity.Id);
            entry.PublicationId = identity.Id;
            entry.PublicationFingerprint = identity.Fingerprint;
        }
        return entry;
    }

    Task<DbContextScope<EventSequenceDbContext>> OpenNamespaceEvents() => database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

    void AddNamespaceEvents(EventSequenceDbContext destination, EventToAppendToStorage[] batch, bool persistIdentities)
    {
        foreach (var item in batch)
        {
            destination.Events.Add(NamespaceEntry(item, persistIdentities));
            destination.NamedTags.AddRange(item.NamedTags.Select((tag, position) => NamedTagEntry.From(eventSequenceId.Value, item.SequenceNumber.Value, position, tag)));
        }
    }

    async Task<AppendedEvent[]> PrepareNamespaceEvents(EventToAppendToStorage[] batch)
    {
        var appended = new List<AppendedEvent>();
        foreach (var item in batch)
        {
            var hash = item.ContentHashes.TryGetValue(item.EventType.Generation, out var contentHash) ? contentHash : EventHash.NotSet;
            var context = new EventContext(item.EventType, item.EventSourceType, item.EventSourceId, item.EventStreamType, item.EventStreamId, item.SequenceNumber, TruncateToMicrosecond(item.Occurred), eventStore, @namespace, item.CorrelationId, item.Causation, await identityStorage.GetFor(item.CausedByChain), item.Tags, hash, Subject: item.Subject?.IsSet == true ? item.Subject : new Subject(item.EventSourceId.Value))
            {
                NamedTags = item.NamedTags,
                EventSource = item.EventSource,
                CompensationFor = item.CompensationFor,
                AppendedGeneration = item.EventType.Generation
            };
            appended.Add(new AppendedEvent(context, item.GenerationalContent[item.EventType.Generation])
            {
                GenerationalContent = EventEntryConverter.BuildGenerationalContent(item.GenerationalContent),
                GenerationalHashes = item.ContentHashes.ToDictionary(pair => (int)pair.Key.Value, pair => pair.Value)
            });
        }
        return appended.ToArray();
    }
}
