// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Represents an implementation of <see cref="IClosedStreamsConstraintStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="eventSequenceId">The event sequence.</param>
/// <param name="database">The database.</param>
public class ClosedStreamsConstraintStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database) : IClosedStreamsConstraintStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetCovering(ClosedStreamScope target, IEnumerable<ClosedStreamDimensions> masks)
    {
        target = target.Normalized();
        var parameter = Expression.Parameter(typeof(ClosedStreamEntry), "row");
        Expression clauses = Expression.Constant(false);
        foreach (var mask in masks.Distinct().Where(mask => mask != ClosedStreamDimensions.None && (mask & target.Dimensions) == mask))
        {
            var scope = ClosedStreamScope.ForAppend(target.EventSourceId ?? EventSourceId.Unspecified, target.EventSourceType ?? EventSourceType.Unspecified, target.EventStreamType ?? EventStreamType.All, target.EventStreamId ?? EventStreamId.Default, mask);
            var clause = ExactScope(scope);
            clauses = Expression.OrElse(clauses, new ReplaceParameter(clause.Parameters[0], parameter).Visit(clause.Body));
        }

        await using var context = await database.Namespace(eventStore, @namespace);
        var predicate = Expression.Lambda<Func<ClosedStreamEntry, bool>>(clauses, parameter);
        var rows = await ForSequence(context.DbContext).Where(predicate).ToArrayAsync();

        return rows.Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStreamDimensions>> GetDimensionsInUse()
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var masks = await ForSequence(context.DbContext).Select(row => row.Dimensions).Distinct().ToArrayAsync();

        return masks.Cast<ClosedStreamDimensions>().ToArray();
    }

    /// <inheritdoc/>
    public async Task Close(ClosedStream closure)
    {
        var scope = closure.Scope.Normalized();
        await using var context = await database.Namespace(eventStore, @namespace);
        var row = await ForSequence(context.DbContext).Where(row => row.Owner == closure.Owner.Value).FirstOrDefaultAsync(ExactScope(scope));
        if (row is null)
        {
            row = new ClosedStreamEntry
            {
                EventSequenceId = eventSequenceId.Value,
                StreamType = scope.EventStreamType?.Value ?? string.Empty,
                StreamId = scope.EventStreamId?.Value ?? string.Empty,
                EventSourceId = scope.EventSourceId?.Value ?? string.Empty,
                EventSourceType = scope.EventSourceType?.Value ?? string.Empty,
                Owner = closure.Owner.Value,
                Dimensions = (int)scope.Dimensions
            };
            context.DbContext.ClosedStreams.Add(row);
        }

        row.SequenceNumber = closure.SequenceNumber.Value;
        row.ClosedAt = closure.ClosedAt;
        await context.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> Reopen(ClosedStreamOwner owner, ClosedStreamScope scope)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var rows = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).Where(ExactScope(scope.Normalized())).ToArrayAsync();
        context.DbContext.ClosedStreams.RemoveRange(rows);
        await context.DbContext.SaveChangesAsync();

        return rows.Length > 0;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetForOwner(ClosedStreamOwner owner)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var rows = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).ToArrayAsync();

        return rows.Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task RemoveAllFor(ClosedStreamOwner owner)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var rows = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).ToArrayAsync();
        context.DbContext.ClosedStreams.RemoveRange(rows);
        await context.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetAll(ClosedStreamScope? within = default, int skip = 0, int? take = default)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var query = ForSequence(context.DbContext);
        if (within is not null)
        {
            var scope = within.Normalized();
            if (scope.IsEmpty) return [];
            if (scope.EventSourceId is not null) query = query.Where(row => row.EventSourceId == scope.EventSourceId.Value);
            if (scope.EventSourceType is not null) query = query.Where(row => row.EventSourceType == scope.EventSourceType.Value);
            if (scope.EventStreamType is not null) query = query.Where(row => row.StreamType == scope.EventStreamType.Value);
            if (scope.EventStreamId is not null) query = query.Where(row => row.StreamId == scope.EventStreamId.Value);
        }

        query = query.OrderBy(row => row.Owner).ThenBy(row => row.EventSourceId).ThenBy(row => row.EventSourceType)
            .ThenBy(row => row.StreamType).ThenBy(row => row.StreamId).Skip(skip);
        if (take is not null) query = query.Take(take.Value);

        return (await query.ToArrayAsync()).Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> IsStreamClosed(EventStreamType streamType, EventStreamId streamId) =>
        (await GetCovering(new(EventStreamType: streamType, EventStreamId: streamId), await GetDimensionsInUse())).Any();

    /// <inheritdoc/>
    public Task CloseStream(EventStreamType streamType, EventStreamId streamId) =>
        Close(new(new(EventStreamType: streamType, EventStreamId: streamId), ClosedStreamOwner.Manual, EventSequenceNumber.Unavailable, null));

    static Expression<Func<ClosedStreamEntry, bool>> ExactScope(ClosedStreamScope scope)
    {
        var sourceId = scope.EventSourceId?.Value ?? string.Empty;
        var sourceType = scope.EventSourceType?.Value ?? string.Empty;
        var streamType = scope.EventStreamType?.Value ?? string.Empty;
        var streamId = scope.EventStreamId?.Value ?? string.Empty;

        var dimensions = (int)scope.Dimensions;

        return row => row.EventSourceId == sourceId && row.EventSourceType == sourceType && row.StreamType == streamType && row.StreamId == streamId && row.Dimensions == dimensions;
    }

    static ClosedStream ToClosure(ClosedStreamEntry row) => new(
        new(
            row.EventSourceId.Length == 0 ? null : new EventSourceId(row.EventSourceId),
            row.EventSourceType.Length == 0 ? null : new EventSourceType(row.EventSourceType),
            ((ClosedStreamDimensions)row.Dimensions).HasFlag(ClosedStreamDimensions.EventStreamType) ? new EventStreamType(row.StreamType) : null,
            ((ClosedStreamDimensions)row.Dimensions).HasFlag(ClosedStreamDimensions.EventStreamId) ? new EventStreamId(row.StreamId) : null),
        row.Owner,
        new EventSequenceNumber((ulong)row.SequenceNumber),
        row.ClosedAt);

    IQueryable<ClosedStreamEntry> ForSequence(NamespaceDbContext context) =>
        context.ClosedStreams.Where(row => row.EventSequenceId == eventSequenceId.Value);

    sealed class ReplaceParameter(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : node;
    }
}
