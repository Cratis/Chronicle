// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    const int MaxRevisionAttempts = 16;

    async Task ReviseWithHistory(EventSequenceNumber sequenceNumber, EventTypeGeneration generation, ExpandoObject content, EventRevisionEntry revision)
    {
        for (var attempt = 0; attempt < MaxRevisionAttempts; attempt++)
        {
            if (await TryRevise(sequenceNumber, generation, content, revision)) return;
        }
        throw new ConcurrentEventRevision(sequenceNumber);
    }

    async Task<bool> TryRevise(EventSequenceNumber sequenceNumber, EventTypeGeneration generation, ExpandoObject content, EventRevisionEntry revision)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var entry = await scope.DbContext.Events.AsNoTracking().Where(_ => _.SequenceNumber == sequenceNumber.Value)
            .Select(_ => new EventEntry { Type = _.Type, Content = _.Content, ContentHashes = _.ContentHashes, RevisedGeneration = _.RevisedGeneration, OriginalContent = _.OriginalContent, Revisions = _.Revisions })
            .FirstOrDefaultAsync();
        if (entry is null || entry.Type == GlobalEventTypes.Redaction) return true;

        var oldContent = entry.Content;
        var oldHashes = entry.ContentHashes;
        var oldRevisedGeneration = entry.RevisedGeneration;
        var oldOriginal = entry.OriginalContent;
        var oldRevisions = entry.Revisions;
        if (oldRevisedGeneration is null && oldOriginal is null && oldRevisions is null)
        {
            entry.OriginalContent = RevisionHistory.CaptureOriginal(entry);
        }
        entry.Revisions = RevisionHistory.Append(oldRevisions, revision);
        EventEntryConverter.UpdateContentForGeneration(entry, generation, content);
        EventEntryConverter.UpdateHashForGeneration(entry, generation, new EventHash(revision.Hash!));
        var updated = await scope.DbContext.Events
            .Where(_ => _.SequenceNumber == sequenceNumber.Value && _.Type == entry.Type
                && _.Content == oldContent && _.ContentHashes == oldHashes
                && _.RevisedGeneration == oldRevisedGeneration && _.OriginalContent == oldOriginal && _.Revisions == oldRevisions)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(_ => _.Content, entry.Content)
                .SetProperty(_ => _.ContentHashes, entry.ContentHashes)
                .SetProperty(_ => _.RevisedGeneration, generation.Value)
                .SetProperty(_ => _.OriginalContent, entry.OriginalContent)
                .SetProperty(_ => _.Revisions, entry.Revisions));
        return updated == 1;
    }
}
