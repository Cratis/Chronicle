// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>Selects the current event without loading revision history.</summary>
public static class EventEntryProjection
{
    static readonly Expression<Func<EventEntry, EventEntry>> _current = entry => new EventEntry
    {
        SequenceNumber = entry.SequenceNumber,
        CorrelationId = entry.CorrelationId,
        Causation = entry.Causation,
        CausedBy = entry.CausedBy,
        Type = entry.Type,
        AppendedGeneration = entry.AppendedGeneration,
        RevisedGeneration = entry.RevisedGeneration,
        Occurred = entry.Occurred,
        EventSourceType = entry.EventSourceType,
        EventSourceId = entry.EventSourceId,
        EventStreamType = entry.EventStreamType,
        EventStreamId = entry.EventStreamId,
        Content = entry.Content,
        ContentHashes = entry.ContentHashes,
        DerivedGenerations = entry.DerivedGenerations,
        Compensations = entry.Compensations,
        Subject = entry.Subject,
        Tags = entry.Tags,
        EventSource = entry.EventSource,
        CompensationFor = entry.CompensationFor,
        PublicationIdentityHash = entry.PublicationIdentityHash,
        PublicationId = entry.PublicationId,
        PublicationFingerprint = entry.PublicationFingerprint
    };

    /// <summary>Projects every ordinary event column, excluding the nullable history columns.</summary>
    /// <param name="entries">The source query.</param>
    /// <returns>The current-only event query.</returns>
    public static IQueryable<EventEntry> WithoutHistory(this IQueryable<EventEntry> entries) => entries.Select(_current);
}
