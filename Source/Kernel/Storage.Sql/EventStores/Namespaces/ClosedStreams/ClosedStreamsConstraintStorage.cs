// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Linq.Expressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;

/// <summary>
/// Represents an implementation of <see cref="IClosedStreamsConstraintStorage"/> for SQL.
/// </summary>
/// <param name="eventStore">The event store.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="eventSequenceId">The event sequence.</param>
/// <param name="database">The database.</param>
/// <param name="logger">Optional logger; no logging when not given.</param>
public class ClosedStreamsConstraintStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IDatabase database, ILogger<ClosedStreamsConstraintStorage>? logger = default) : IClosedStreamsConstraintStorage
{
    static readonly ConcurrentDictionary<(string Store, string Namespace, string Sequence), bool> _reported = new();
    readonly SqlStorageTelemetry _telemetry = SqlStorageTelemetry.For(database);
    readonly ILogger<ClosedStreamsConstraintStorage> _logger = logger ?? NullLogger<ClosedStreamsConstraintStorage>.Instance;

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetCovering(ClosedStreamScope target, IEnumerable<ClosedStreamDimensions> masks)
    {
        target = target.Normalized();
        var parameter = Expression.Parameter(typeof(ClosedStreamEntry), "row");
        Expression clauses = Expression.Constant(false);
        var scopes = new List<ClosedStreamScope>();
        foreach (var mask in masks.Distinct().Where(mask => mask != ClosedStreamDimensions.None && (mask & target.Dimensions) == mask))
        {
            var scope = ClosedStreamScope.ForAppend(target.EventSourceId ?? EventSourceId.Unspecified, target.EventSourceType ?? EventSourceType.Unspecified, target.EventStreamType ?? EventStreamType.All, target.EventStreamId ?? EventStreamId.Default, mask);
            scopes.Add(scope);
            var clause = ExactScope(scope);
            clauses = Expression.OrElse(clauses, new ReplaceParameter(clause.Parameters[0], parameter).Visit(clause.Body));
        }

        await using var context = await database.Namespace(eventStore, @namespace);
        var predicate = Expression.Lambda<Func<ClosedStreamEntry, bool>>(clauses, parameter);
        var found = await ForSequence(context.DbContext).Where(predicate).ToArrayAsync();
        var rows = found.Where(row => scopes.Exists(scope => IsOrdinalMatch(row, scope) || !row.ExactSpelling)).ToArray();
        ReportPending(rows, scopes);

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
    public async Task Close(ClosedStream closure, CancellationToken cancellationToken = default)
    {
        var scope = closure.Scope.Normalized();
        await using var context = await database.Namespace(eventStore, @namespace);
        var candidates = await ForSequence(context.DbContext).Where(row => row.Owner == closure.Owner.Value).Where(ExactScope(scope)).ToArrayAsync(cancellationToken);
        var row = candidates.FirstOrDefault(candidate => IsOrdinalMatch(candidate, scope));
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
                Dimensions = (int)scope.Dimensions,
                ExactSpelling = true
            };
            context.DbContext.ClosedStreams.Add(row);
        }
        else
        {
            context.DbContext.ClosedStreams.Attach(row);
        }

        row.SequenceNumber = closure.SequenceNumber.Value;
        row.ClosedAt = closure.ClosedAt;
        await context.DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> Reopen(ClosedStreamOwner owner, ClosedStreamScope scope, CancellationToken cancellationToken = default)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var normalized = scope.Normalized();
        var found = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).Where(ExactScope(normalized)).ToArrayAsync(cancellationToken);
        var matching = found.Where(row => IsOrdinalMatch(row, normalized)).ToArray();

        // Entries written before identifier comparison became exact stay closed until they have been reviewed.
        var rows = matching.Where(row => row.ExactSpelling).ToArray();
        ReportPending(found.Where(row => !row.ExactSpelling), []);
        context.DbContext.ClosedStreams.RemoveRange(rows);
        await context.DbContext.SaveChangesAsync(cancellationToken);

        return rows.Length > 0;
    }

    /// <inheritdoc/>
    public async Task<ClosedStream?> GetFor(ClosedStreamOwner owner, ClosedStreamScope scope, CancellationToken cancellationToken = default)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var normalized = scope.Normalized();
        var found = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).Where(ExactScope(normalized)).ToArrayAsync(cancellationToken);
        var row = found.FirstOrDefault(candidate => IsOrdinalMatch(candidate, normalized)) ?? found.FirstOrDefault(candidate => !candidate.ExactSpelling);
        if (row is not null && !IsOrdinalMatch(row, normalized))
        {
            ReportPending([row], [normalized]);
        }

        return row is null ? null : ToClosure(row);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetForEvent(ClosedStreamOwner owner, EventSequenceNumber sequenceNumber, CancellationToken cancellationToken = default)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var position = (decimal)sequenceNumber.Value;
        var rows = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value && row.SequenceNumber == position).ToArrayAsync(cancellationToken);

        return rows.Select(ToClosure).ToArray();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ClosedStream>> GetForOwner(ClosedStreamOwner owner, CancellationToken cancellationToken = default)
    {
        await using var context = await database.Namespace(eventStore, @namespace);
        var rows = await ForSequence(context.DbContext).Where(row => row.Owner == owner.Value).ToArrayAsync(cancellationToken);

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
            .ThenBy(row => row.StreamType).ThenBy(row => row.StreamId);

        // Only SQL Server compares identifiers loosely, so only there are the rows checked again for an exact match
        // before they are paged. Other providers page in the query exactly as before.
        var exactInMemory = within is not null && context.DbContext.Database.IsSqlServer();
        if (!exactInMemory)
        {
            query = query.Skip(skip);
            if (take is not null) query = query.Take(take.Value);
        }

        var all = await query.ToArrayAsync();
        if (within is not null)
        {
            var scope = within.Normalized();
            if (exactInMemory)
            {
                all = [.. all.Where(row => !row.ExactSpelling || IsWithin(row, scope)).Skip(skip).Take(take ?? int.MaxValue)];
            }

            ReportPending(all, [scope]);
        }

        return all.Select(ToClosure).ToArray();
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

    static bool IsOrdinalMatch(ClosedStreamEntry row, ClosedStreamScope scope) =>
        string.Equals(row.EventSourceId, scope.EventSourceId?.Value ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(row.EventSourceType, scope.EventSourceType?.Value ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(row.StreamType, scope.EventStreamType?.Value ?? string.Empty, StringComparison.Ordinal)
        && string.Equals(row.StreamId, scope.EventStreamId?.Value ?? string.Empty, StringComparison.Ordinal);

    static bool IsWithin(ClosedStreamEntry row, ClosedStreamScope scope) =>
        (scope.EventSourceId is null || string.Equals(row.EventSourceId, scope.EventSourceId.Value, StringComparison.Ordinal))
        && (scope.EventSourceType is null || string.Equals(row.EventSourceType, scope.EventSourceType.Value, StringComparison.Ordinal))
        && (scope.EventStreamType is null || string.Equals(row.StreamType, scope.EventStreamType.Value, StringComparison.Ordinal))
        && (scope.EventStreamId is null || string.Equals(row.StreamId, scope.EventStreamId.Value, StringComparison.Ordinal));

    static ClosedStream ToClosure(ClosedStreamEntry row) => new(
        new(
            row.EventSourceId.Length == 0 ? null : new EventSourceId(row.EventSourceId),
            row.EventSourceType.Length == 0 ? null : new EventSourceType(row.EventSourceType),
            ((ClosedStreamDimensions)row.Dimensions).HasFlag(ClosedStreamDimensions.EventStreamType) ? new EventStreamType(row.StreamType) : null,
            ((ClosedStreamDimensions)row.Dimensions).HasFlag(ClosedStreamDimensions.EventStreamId) ? new EventStreamId(row.StreamId) : null),
        row.Owner,
        new EventSequenceNumber((ulong)row.SequenceNumber),
        row.ClosedAt);

    void ReportPending(IEnumerable<ClosedStreamEntry> rows, IReadOnlyCollection<ClosedStreamScope> scopes)
    {
        var count = rows.Count(row => !row.ExactSpelling && !scopes.Any(scope => IsOrdinalMatch(row, scope)));
        if (count > 0 && _reported.TryAdd((eventStore.Value, @namespace.Value, eventSequenceId.Value), true))
        {
            _telemetry.ClosedStreamEntriesPendingReview(count);
            _logger.ClosedStreamEntriesPendingReview(count, eventSequenceId.Value);
        }
    }

    IQueryable<ClosedStreamEntry> ForSequence(NamespaceDbContext context) =>
        context.ClosedStreams.AsNoTracking().Where(row => row.EventSequenceId == eventSequenceId.Value);

    sealed class ReplaceParameter(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : node;
    }
}
