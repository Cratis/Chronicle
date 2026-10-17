// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendBatch(IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        EventToAppendToStorage[] eventsArray;
        try
        {
            eventsArray = events.ToArray();
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
        if (eventsArray.Length == 0)
        {
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success([]);
        }

        var writeIssued = false;
        try
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
            var appendedEvents = new List<AppendedEvent>();

            foreach (var eventToAppend in eventsArray)
            {
                // Same precision-cap rationale as the single Append path: keep the value the
                // projection observes equal to what GetForEventSourceIdAndEventTypes returns
                // later. See the comment in Append for details.
                var truncatedOccurred = TruncateToMicrosecond(eventToAppend.Occurred);

                // Check if sequence number already exists
                var appendManySeqNum = eventToAppend.SequenceNumber.Value;
                var existingEvent = await scope.DbContext.Events
                    .FirstOrDefaultAsync(e => e.SequenceNumber == appendManySeqNum);

                if (existingEvent is not null)
                {
                    // Keyed batches must settle identities before classifying an occupied slot.
                    if (persistIdentities && await ClassifyIdentityCollision(eventsArray) is { } collision)
                    {
                        return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(collision);
                    }
                    return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(new DuplicateEventSequenceNumber(await GetNextAvailableSequenceNumber(scope)));
                }

                var eventEntry = EventEntryConverter.ToEventEntry(
                    eventToAppend.SequenceNumber,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.EventStreamType,
                    eventToAppend.EventStreamId,
                    eventToAppend.EventType,
                    eventToAppend.CorrelationId,
                    eventToAppend.Causation,
                    eventToAppend.CausedByChain,
                    eventToAppend.Tags,
                    truncatedOccurred,
                    eventToAppend.GenerationalContent,
                    eventToAppend.ContentHashes,
                    eventToAppend.Subject?.IsSet == true ? eventToAppend.Subject : null);
                eventEntry.EventSource = eventToAppend.EventSource.IsSet ? eventToAppend.EventSource.Value : null;
                if (persistIdentities && eventToAppend.Identity is { } identity)
                {
                    eventEntry.PublicationIdentityHash = PublicationIdentityHash(identity.Id);
                    eventEntry.PublicationId = identity.Id;
                    eventEntry.PublicationFingerprint = identity.Fingerprint;
                }

                scope.DbContext.Events.Add(eventEntry);
                scope.DbContext.NamedTags.AddRange(eventToAppend.NamedTags.Select((tag, position) => NamedTagEntry.From(eventSequenceId.Value, eventToAppend.SequenceNumber.Value, position, tag)));

                var resolvedSubject = eventToAppend.Subject?.IsSet == true
                    ? eventToAppend.Subject
                    : new Subject(eventToAppend.EventSourceId.Value);
                var appendedHash = eventToAppend.ContentHashes.TryGetValue(eventToAppend.EventType.Generation, out var contentHash) ? contentHash : EventHash.NotSet;
                var eventContext = new EventContext(
                    eventToAppend.EventType,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.EventStreamType,
                    eventToAppend.EventStreamId,
                    eventToAppend.SequenceNumber,
                    truncatedOccurred,
                    eventStore,
                    @namespace,
                    eventToAppend.CorrelationId,
                    eventToAppend.Causation,
                    await identityStorage.GetFor(eventToAppend.CausedByChain),
                    eventToAppend.Tags,
                    appendedHash,
                    Subject: resolvedSubject)
                {
                    NamedTags = eventToAppend.NamedTags,
                    EventSource = eventToAppend.EventSource,
                    AppendedGeneration = eventToAppend.EventType.Generation
                };

                appendedEvents.Add(new AppendedEvent(eventContext, eventToAppend.GenerationalContent[eventToAppend.EventType.Generation])
                {
                    GenerationalContent = EventEntryConverter.BuildGenerationalContent(eventToAppend.GenerationalContent),
                    GenerationalHashes = eventToAppend.ContentHashes.ToDictionary(_ => (int)_.Key.Value, _ => _.Value)
                });
            }

            writeIssued = true;
            await scope.DbContext.SaveChangesAsync();
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(appendedEvents);
        }
        catch (DbUpdateException error) when (persistIdentities && IsAppendUniqueViolation(error))
        {
            // SaveChanges has rolled back; fresh scopes must not read attempted tracked inserts.
            if (await ClassifyIdentityCollision(eventsArray) is { } collision)
            {
                return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(collision);
            }
            throw;
        }
        catch (DbUpdateException error) when (!persistIdentities && IsAppendUniqueViolation(error))
        {
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(new DuplicateEventSequenceNumber((await GetTailSequenceNumber()).Next()));
        }
        catch (Exception error) when (persistIdentities && StorageWriteConflicts.IsTransactionConflict(error))
        {
            throw new EventsNotCommitted(error);
        }
        catch (Exception ex)
        {
            logger.FailedToAppendEvent(ex, EventSequenceNumber.Unavailable, eventSequenceId);
            if (!writeIssued) throw new EventsNotCommitted(ex);
            throw;
        }
    }
}
