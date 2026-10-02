// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts;

/// <summary>
/// Provides acknowledged atomic conditional writes and scoped incident reads on standalone MongoDB.
/// </summary>
/// <param name="database">The namespace database.</param>
public class AlertIncidentsStorage(IEventStoreNamespaceDatabase database) : IAlertIncidentsStorage
{
    readonly Lazy<IMongoCollection<AlertIncidentDocument>> _collection = new(() => AcknowledgedCollection(database));
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <inheritdoc/>
    public async Task<AlertIncidentWriteOutcome> Apply(AlertIncidentTransition transition, CancellationToken cancellationToken = default)
    {
        AlertIncidentStorageRules.Validate(transition.SequenceNumber);
        await EnsureIndexes();
        var row = AlertIncidentFold.Apply(null, transition).Incident;

        // Escalations do not have an insert row; flatten their values without consulting storage.
        row ??= new(transition.Id, transition.Target, transition.Condition, transition.Severity, transition.Evidence, null, null, transition.Occurred, transition.SequenceNumber, true, null);
        var document = row.ToMongoDB();
        var filters = Builders<AlertIncidentDocument>.Filter;
        var filter = filters.Eq(_ => _.Id, document.Id) & filters.Lt(_ => _.LastTransitionSequenceNumber, document.LastTransitionSequenceNumber);
        var updates = Builders<AlertIncidentDocument>.Update;
        var update = updates.Set(_ => _.Condition, document.Condition)
            .Set(_ => _.LastChangedAt, document.LastChangedAt)
            .Set(_ => _.LastTransitionSequenceNumber, document.LastTransitionSequenceNumber);
        if (transition.Kind == AlertIncidentTransitionKind.Escalated)
        {
            filter &= filters.Eq(_ => _.IsOpen, true);
        }
        else
        {
            update = update.Set(_ => _.EventStore, document.EventStore).Set(_ => _.Namespace, document.Namespace)
                .Set(_ => _.ObserverId, document.ObserverId).Set(_ => _.EventSequenceId, document.EventSequenceId)
                .Set(_ => _.Partition, document.Partition).Set(_ => _.IsOpen, document.IsOpen)
                .Set(_ => _.ClearedReason, document.ClearedReason);
        }
        if (transition.Kind == AlertIncidentTransitionKind.Cleared)
        {
            update = update.SetOnInsert(_ => _.Severity, null).SetOnInsert(_ => _.AttemptCount, null)
                .SetOnInsert(_ => _.FirstFailure, null).SetOnInsert(_ => _.LastFailure, null)
                .SetOnInsert(_ => _.FailureKind, null).SetOnInsert(_ => _.Message, null)
                .SetOnInsert(_ => _.RaisedAt, null).SetOnInsert(_ => _.RaisedSequenceNumber, null);
        }
        else
        {
            update = update.Set(_ => _.Severity, document.Severity).Set(_ => _.AttemptCount, document.AttemptCount)
                .Set(_ => _.FirstFailure, document.FirstFailure).Set(_ => _.LastFailure, document.LastFailure)
                .Set(_ => _.FailureKind, document.FailureKind).Set(_ => _.Message, document.Message);
            if (transition.Kind == AlertIncidentTransitionKind.Raised)
            {
                update = update.Set(_ => _.RaisedAt, document.RaisedAt).Set(_ => _.RaisedSequenceNumber, document.RaisedSequenceNumber);
            }
        }
        try
        {
            var result = await _collection.Value.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = transition.Kind != AlertIncidentTransitionKind.Escalated, Collation = Collation.Simple }, cancellationToken);
            if (!result.IsAcknowledged)
            {
                throw new AlertIncidentWriteNotConfirmed(transition.Id);
            }
            if (result.MatchedCount > 0 || result.UpsertedId is not null)
            {
                return AlertIncidentWriteOutcome.Applied;
            }
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Another insert may have won with an older position. Retry the same guard without inserting.
            var result = await _collection.Value.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = false, Collation = Collation.Simple }, cancellationToken);
            if (!result.IsAcknowledged)
            {
                throw new AlertIncidentWriteNotConfirmed(transition.Id);
            }
            if (result.MatchedCount > 0)
            {
                return AlertIncidentWriteOutcome.Applied;
            }
        }
        var current = await _collection.Value.Find(_ => _.Id == document.Id, new FindOptions { Collation = Collation.Simple })
            .FirstOrDefaultAsync(cancellationToken);

        return AlertIncidentStorageRules.Confirm(current?.ToKernel(), transition);
    }

    /// <inheritdoc/>
    public async Task<AlertIncident?> GetOpen(AlertIncidentScope scope, IncidentId incidentId, CancellationToken cancellationToken = default)
    {
        await EnsureIndexes();
        var row = await _collection.Value.Find(
            Scope(scope) & Builders<AlertIncidentDocument>.Filter.Eq(_ => _.Id, AlertIncidentStorageRules.Key(incidentId)),
            new FindOptions { Collation = Collation.Simple }).FirstOrDefaultAsync(cancellationToken);

        return row?.ToKernel();
    }

    /// <inheritdoc/>
    public Task<AlertIncidentStoragePage> GetOpenPage(AlertIncidentFilter filter, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default)
    {
        var predicate = Scope(filter.Scope);
        var builder = Builders<AlertIncidentDocument>.Filter;
        if (filter.ObserverId is not null) predicate &= builder.Eq(_ => _.ObserverId, filter.ObserverId.Value);
        if (filter.Condition is not null) predicate &= builder.Eq(_ => _.Condition, filter.Condition.Value);
        if (filter.MinimumSeverity is not null) predicate &= builder.Gte(_ => _.Severity, (int)filter.MinimumSeverity.Value);

        return Page(predicate, after, limit, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AlertIncidentCount>> GetOpenCounts(AlertIncidentScope scope, CancellationToken cancellationToken = default)
    {
        await EnsureIndexes();
        var counts = await _collection.Value.Aggregate(new AggregateOptions { Collation = Collation.Simple }).Match(Scope(scope))
            .Group(row => new { row.Namespace, row.Condition, row.Severity }, group => new
            {
                group.Key.Namespace,
                group.Key.Condition,
                group.Key.Severity,
                Count = group.LongCount()
            }).ToListAsync(cancellationToken);

        return counts.Select(_ => new AlertIncidentCount(_.Namespace, _.Condition, (AlertSeverity)_.Severity!.Value, _.Count)).ToArray();
    }

    /// <inheritdoc/>
    public Task<AlertIncidentStoragePage> EnumerateOpen(AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default) =>
        Page(Builders<AlertIncidentDocument>.Filter.Eq(_ => _.IsOpen, true), after, limit, cancellationToken);

    /// <summary>
    /// Gets the incident collection, reading from the primary and keeping the configured write concern unless it is unacknowledged.
    /// </summary>
    /// <param name="database">The namespace database.</param>
    /// <returns>The incident collection to read and write through.</returns>
    internal static IMongoCollection<AlertIncidentDocument> AcknowledgedCollection(IEventStoreNamespaceDatabase database)
    {
        var collection = database.GetCollection<AlertIncidentDocument>(WellKnownCollectionNames.AlertIncidents)
            .WithReadPreference(ReadPreference.Primary);

        return collection.Settings.WriteConcern?.IsAcknowledged == false
            ? collection.WithWriteConcern(WriteConcern.Acknowledged)
            : collection;
    }

    static FilterDefinition<AlertIncidentDocument> Scope(AlertIncidentScope scope)
    {
        var builder = Builders<AlertIncidentDocument>.Filter;
        var filter = builder.Eq(_ => _.IsOpen, true) & builder.Eq(_ => _.EventStore, scope.EventStore.Value);
        if (scope.Namespace is not null) filter &= builder.Eq(_ => _.Namespace, scope.Namespace.Value);

        return filter;
    }

    async Task<AlertIncidentStoragePage> Page(FilterDefinition<AlertIncidentDocument> filter, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken)
    {
        limit = AlertIncidentStorageRules.Limit(limit);
        if (after is not null)
        {
            AlertIncidentStorageRules.Validate(after.RaisedSequenceNumber);
            var builder = Builders<AlertIncidentDocument>.Filter;
            var number = (decimal)after.RaisedSequenceNumber.Value;
            filter &= builder.Gt(_ => _.RaisedSequenceNumber, number) |
                (builder.Eq(_ => _.RaisedSequenceNumber, number) & builder.Gt(_ => _.Id, AlertIncidentStorageRules.Key(after.IncidentId)));
        }
        await EnsureIndexes();
        var rows = await _collection.Value.Find(filter, new FindOptions { Collation = Collation.Simple })
            .Sort(Builders<AlertIncidentDocument>.Sort.Ascending(_ => _.RaisedSequenceNumber).Ascending(_ => _.Id))
            .Limit(limit + 1).ToListAsync(cancellationToken);
        var items = rows.Take(limit).Select(_ => _.ToKernel()).ToArray();

        return new(items, rows.Count > limit ? new(items[^1].RaisedSequenceNumber!, items[^1].Id) : null);
    }

    Task EnsureIndexes() => _collection.Value.EnsureIndexesOnceAsync(
        _ensuredIndexes,
        new(Builders<AlertIncidentDocument>.IndexKeys.Ascending(_ => _.EventStore).Ascending(_ => _.IsOpen).Ascending(_ => _.RaisedSequenceNumber).Ascending(_ => _.Id), new CreateIndexOptions { Name = "store-open-raised-id", Collation = Collation.Simple }),
        new(Builders<AlertIncidentDocument>.IndexKeys.Ascending(_ => _.EventStore).Ascending(_ => _.Namespace).Ascending(_ => _.IsOpen).Ascending(_ => _.RaisedSequenceNumber).Ascending(_ => _.Id), new CreateIndexOptions { Name = "store-namespace-open-raised-id", Collation = Collation.Simple }));
}
