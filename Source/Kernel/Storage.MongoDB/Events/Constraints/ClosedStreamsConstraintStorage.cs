// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents an implementation of <see cref="IClosedStreamsConstraintStorage"/> for MongoDB.
/// </summary>
/// <param name="eventStoreNamespaceDatabase">The namespace database.</param>
/// <param name="eventSequenceId">The event sequence.</param>
public class ClosedStreamsConstraintStorage(IEventStoreNamespaceDatabase eventStoreNamespaceDatabase, EventSequenceId eventSequenceId) : IClosedStreamsConstraintStorage
{
    readonly IMongoCollection<ClosedStreamDocument> _collection =
        eventStoreNamespaceDatabase.GetCollection<ClosedStreamDocument>($"{eventSequenceId}+closed_streams");
    bool _indexEnsured;

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetCovering(ClosedStreamScope target, IEnumerable<ClosedStreamDimensions> masks)
    {
        target = target.Normalized();
        var clauses = masks.Distinct().Where(mask => mask != ClosedStreamDimensions.None && (mask & target.Dimensions) == mask)
            .Select(mask => ExactScope(ClosedStreamScope.ForAppend(
                target.EventSourceId ?? EventSourceId.Unspecified,
                target.EventSourceType ?? EventSourceType.Unspecified,
                target.EventStreamType ?? EventStreamType.All,
                target.EventStreamId ?? EventStreamId.Default,
                mask))).ToArray();
        if (clauses.Length == 0)
        {
            return [];
        }

        using var cursor = await _collection.FindAsync(Builders<ClosedStreamDocument>.Filter.Or(clauses));

        return (await cursor.ToListAsync()).Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStreamDimensions>> GetDimensionsInUse()
    {
        await EnsureIndex();

        // Distinct omits missing fields. Project first so legacy documents contribute the stream-only mask.
        var masks = await _collection.Aggregate().Project(row => new { Dimensions = row.Dimensions ?? 12 })
            .Group(row => row.Dimensions, group => new { Dimensions = group.Key }).ToListAsync();

        return masks.Select(row => (ClosedStreamDimensions)row.Dimensions).ToArray();
    }

    /// <inheritdoc/>
    public async Task Close(ClosedStream closure)
    {
        await EnsureIndex();
        var scope = closure.Scope.Normalized();
        await _collection.ReplaceOneAsync(
            ExactScope(scope) & ForOwner(closure.Owner),
            new ClosedStreamDocument(scope.EventStreamType?.Value, scope.EventStreamId?.Value, scope.EventSourceId?.Value, scope.EventSourceType?.Value, closure.Owner.Value, (int)scope.Dimensions, closure.SequenceNumber.Value, closure.ClosedAt),
            new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public async Task<bool> Reopen(ClosedStreamOwner owner, ClosedStreamScope scope) =>
        (await _collection.DeleteOneAsync(ExactScope(scope.Normalized()) & ForOwner(owner))).DeletedCount > 0;

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetForOwner(ClosedStreamOwner owner)
    {
        using var cursor = await _collection.FindAsync(ForOwner(owner));

        return (await cursor.ToListAsync()).Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ClosedStreamOwner owner) => await _collection.DeleteManyAsync(ForOwner(owner));

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetAll(ClosedStreamScope? within = default, int skip = 0, int? take = default)
    {
        var filter = Builders<ClosedStreamDocument>.Filter.Empty;
        if (within is not null)
        {
            var scope = within.Normalized();
            var builder = Builders<ClosedStreamDocument>.Filter;
            if (scope.EventSourceId is not null) filter &= builder.Eq(row => row.EventSourceId, scope.EventSourceId.Value);
            if (scope.EventSourceType is not null) filter &= builder.Eq(row => row.EventSourceType, scope.EventSourceType.Value);
            if (scope.EventStreamType is not null) filter &= builder.Eq(row => row.StreamType, scope.EventStreamType.Value);
            if (scope.EventStreamId is not null) filter &= builder.Eq(row => row.StreamId, scope.EventStreamId.Value);
            if (scope.IsEmpty) return [];
        }

        var query = _collection.Find(filter).SortBy(row => row.Owner).ThenBy(row => row.EventSourceId)
            .ThenBy(row => row.EventSourceType).ThenBy(row => row.StreamType).ThenBy(row => row.StreamId).Skip(skip);
        if (take is not null)
        {
            if (take.Value <= 0) return [];
            query = query.Limit(take.Value);
        }

        return (await query.ToListAsync()).Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId) =>
        (await GetCovering(new(EventStreamType: streamType, EventStreamId: streamId), await GetDimensionsInUse())).Any();

    /// <inheritdoc/>
    public Task CloseStream(EventStreamType streamType, EventStreamId streamId) =>
        Close(new(new(EventStreamType: streamType, EventStreamId: streamId), ClosedStreamOwner.Manual, EventSequenceNumber.Unavailable, null));

    static ClosedStream ToClosure(ClosedStreamDocument row) => new(
        new ClosedStreamScope(
            row.EventSourceId is null ? null : new EventSourceId(row.EventSourceId),
            row.EventSourceType is null ? null : new EventSourceType(row.EventSourceType),
            row.StreamType is null ? null : new EventStreamType(row.StreamType),
            row.StreamId is null ? null : new EventStreamId(row.StreamId)).Normalized(),
        row.Owner ?? ClosedStreamOwner.Manual.Value,
        row.SequenceNumber is null ? EventSequenceNumber.Unavailable : new EventSequenceNumber(row.SequenceNumber.Value),
        row.ClosedAt);

    static FilterDefinition<ClosedStreamDocument> ExactScope(ClosedStreamScope scope)
    {
        var builder = Builders<ClosedStreamDocument>.Filter;

        return builder.Eq(row => row.StreamType, scope.EventStreamType?.Value) &
            builder.Eq(row => row.StreamId, scope.EventStreamId?.Value) &
            builder.Eq(row => row.EventSourceId, scope.EventSourceId?.Value) &
            builder.Eq(row => row.EventSourceType, scope.EventSourceType?.Value);
    }

    static FilterDefinition<ClosedStreamDocument> ForOwner(ClosedStreamOwner owner)
    {
        var builder = Builders<ClosedStreamDocument>.Filter;

        return owner == ClosedStreamOwner.Manual
            ? builder.Eq(row => row.Owner, owner.Value) | builder.Eq(row => row.Owner, null)
            : builder.Eq(row => row.Owner, owner.Value);
    }

    async Task EnsureIndex()
    {
        if (_indexEnsured) return;
        await _collection.Indexes.CreateOneAsync(new CreateIndexModel<ClosedStreamDocument>(
            Builders<ClosedStreamDocument>.IndexKeys.Ascending(row => row.StreamType).Ascending(row => row.StreamId)
                .Ascending(row => row.EventSourceId).Ascending(row => row.EventSourceType).Ascending(row => row.Owner),
            new CreateIndexOptions { Name = "scope_owner" }));
        _indexEnsured = true;
    }
}
