// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents an inspected closed stream scope.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventStreamType">The stream type.</param>
/// <param name="EventStreamId">The stream identifier.</param>
/// <param name="Origin">The closure origin.</param>
/// <param name="ClosedBy">The owning constraint, or null for manual closures.</param>
/// <param name="SequenceNumber">The closing sequence number.</param>
/// <param name="ClosedAt">The closing timestamp.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record ClosedStream(
    string? EventSourceId,
    string? EventSourceType,
    string? EventStreamType,
    string? EventStreamId,
    ClosedStreamOrigin Origin,
    string? ClosedBy,
    EventSequenceNumber SequenceNumber,
    DateTimeOffset? ClosedAt)
{
    /// <summary>
    /// List closed scopes within optional dimensions.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="queryContextManager">The query context holding paging.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="eventSourceId">The optional event source identifier.</param>
    /// <param name="eventSourceType">The optional event source type.</param>
    /// <param name="eventStreamType">The optional stream type.</param>
    /// <param name="eventStreamId">The optional stream identifier.</param>
    /// <returns>The closed scopes.</returns>
    public static async Task<IEnumerable<ClosedStream>> ClosedStreams(
        IStorage storage,
        IQueryContextManager queryContextManager,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? eventStreamId = default)
    {
        var closures = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetClosedStreamsConstraints(eventSequenceId);
        var scope = ClosedStreamConverters.ToScope(eventSourceId, eventSourceType, eventStreamType, eventStreamId);
        var paging = queryContextManager.Current.Paging;
        var rows = await closures.GetAll(scope.IsEmpty ? null : scope, paging.IsPaged ? paging.Skip : 0, paging.IsPaged ? paging.Size.Value : null);

        return rows.Select(ClosedStreamConverters.ToReadModel).ToArray();
    }
}
