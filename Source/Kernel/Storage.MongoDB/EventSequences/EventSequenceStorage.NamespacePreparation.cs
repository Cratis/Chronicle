// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    async Task<(EventToAppendToStorage[] Batch, Event[] Documents, AppendedEvent[] Appended)> PrepareNamespaceBatch(IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        try
        {
            var batch = events.Select(item => item with
            {
                Occurred = StoredTimestamps.Normalize(item.Occurred),
                Causation = item.Causation.Select(StoredTimestamps.Normalize).ToArray(),
                CausedByChain = item.CausedByChain.ToArray(),
                Tags = item.Tags.ToArray()
            }).ToArray();
            var documents = new List<Event>();
            var appended = new List<AppendedEvent>();
            foreach (var item in batch)
            {
                var document = (await ReceiptEvent(item)) with
                {
                    PublicationId = persistIdentities ? item.Identity?.Id : null,
                    PublicationFingerprint = persistIdentities ? item.Identity?.Fingerprint : null,
                    CompensationFor = item.CompensationFor
                };
                documents.Add(document);
                appended.Add(await NamespaceAppendedEvent(item, document));
            }
            return (batch, documents.ToArray(), appended.ToArray());
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
    }

    async Task<AppendedEvent> NamespaceAppendedEvent(EventToAppendToStorage item, Event document)
    {
        var hash = item.ContentHashes.TryGetValue(item.EventType.Generation, out var value) ? value : EventHash.NotSet;
        var context = new EventContext(
            item.EventType,
            item.EventSourceType,
            item.EventSourceId,
            item.EventStreamType,
            item.EventStreamId,
            item.SequenceNumber,
            item.Occurred,
            eventStore,
            @namespace,
            item.CorrelationId,
            item.Causation,
            await identityStorage.GetFor(item.CausedByChain),
            item.Tags,
            hash,
            Subject: item.Subject?.IsSet == true ? item.Subject : new Subject(item.EventSourceId.Value))
        {
            NamedTags = item.NamedTags,
            EventSource = item.EventSource,
            CompensationFor = item.CompensationFor,
            AppendedGeneration = item.EventType.Generation
        };
        var content = item.GenerationalContent.TryGetValue(item.EventType.Generation, out var generationContent) ? generationContent : item.GenerationalContent.Values.FirstOrDefault() ?? [];
        return new(context, content)
        {
            GenerationalContent = document.Content.ToDictionary(pair => int.Parse(pair.Key), pair => EventContentBson.ToJson(pair.Value)),
            GenerationalHashes = item.ContentHashes.ToDictionary(pair => (int)pair.Key.Value, pair => pair.Value)
        };
    }
}
