// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendBatch(IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        var (eventsToAppend, causedByPerEvent) = await SnapshotBatch(events);
        var appended = new List<AppendedEvent>();

        lock (_lock)
        {
            if (persistIdentities)
            {
                var identities = eventsToAppend.Where(item => item.Identity is not null).Select(item => item.Identity!.Id).ToArray();
                var receipts = FindPublications(identities);
                if (receipts.Count > 0)
                {
                    return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(new IdentitiesAlreadyDurable(receipts));
                }
                if (identities.Distinct(StringComparer.Ordinal).Count() != identities.Length)
                {
                    throw new EventsNotCommitted(new EventPublicationConflict());
                }
            }
            if (FindDuplicateSequenceNumber(eventsToAppend) is { } duplicate)
            {
                return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(duplicate);
            }

            var (stored, metadata) = BuildBatch(eventsToAppend, causedByPerEvent, appended);

            // Publish only after the entire batch, its acknowledgments and metadata have been built.
            for (var index = 0; index < eventsToAppend.Length; index++)
            {
                var e = eventsToAppend[index];
                _originalCausedByChains[e.SequenceNumber] = (IdentityId[])e.CausedByChain;
                _metadata[e.SequenceNumber] = metadata[index];
                _appendedGenerations[e.SequenceNumber] = e.EventType.Generation.Value;
                _generationHashes[e.SequenceNumber] = (Dictionary<EventTypeGeneration, EventHash>)e.ContentHashes;
            }
            _events.AddRange(stored);
            if (persistIdentities)
            {
                foreach (var item in eventsToAppend.Where(item => item.Identity is not null))
                {
                    _publications.Add(item.Identity!.Id, (item.Identity.Fingerprint, new EventPublicationReceipt(item.SequenceNumber)));
                }
            }
        }

        return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(appended);
    }
}
