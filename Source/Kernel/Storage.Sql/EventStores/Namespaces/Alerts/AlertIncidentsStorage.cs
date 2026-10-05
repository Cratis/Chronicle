// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Alerts;

/// <summary>
/// Provides atomic conditional incident mutations and storage-scoped SQL queries.
/// </summary>
/// <param name="eventStore">The physical event store.</param>
/// <param name="namespace">The physical namespace.</param>
/// <param name="database">The database scope provider.</param>
public class AlertIncidentsStorage(EventStoreName eventStore, EventStoreNamespaceName @namespace, IDatabase database) : IAlertIncidentsStorage
{
    /// <inheritdoc/>
    public async Task<AlertIncidentWriteOutcome> Apply(AlertIncidentTransition transition, CancellationToken cancellationToken = default)
    {
        AlertIncidentStorageRules.Validate(transition.SequenceNumber);
        var row = AlertIncidentFold.Apply(null, transition).Incident ?? new(transition.Id, transition.Target, transition.Condition, transition.Severity, transition.Evidence, null, null, transition.Occurred, transition.SequenceNumber, true, null);
        var entity = row.ToSql();
        await using var scope = await database.Namespace(eventStore, @namespace);
        var statement = AlertIncidentStatements.For(scope.DbContext, entity, transition.Kind);
        var affected = await scope.DbContext.Database.ExecuteSqlRawAsync(statement.Sql, statement.Values, cancellationToken);
        if (affected > 0)
        {
            return AlertIncidentWriteOutcome.Applied;
        }
        var current = await scope.DbContext.AlertIncidents.AsNoTracking().FirstOrDefaultAsync(_ => _.Id == entity.Id, cancellationToken);

        return AlertIncidentStorageRules.Confirm(current?.ToKernel(), transition);
    }

    /// <inheritdoc/>
    public async Task<AlertIncident?> GetOpen(AlertIncidentScope scope, IncidentId incidentId, CancellationToken cancellationToken = default)
    {
        await using var databaseScope = await database.Namespace(eventStore, @namespace);
        var id = AlertIncidentStorageRules.Key(incidentId);
        var row = await Scoped(databaseScope.DbContext, scope).FirstOrDefaultAsync(_ => _.Id == id, cancellationToken);

        return row?.ToKernel();
    }

    /// <inheritdoc/>
    public async Task<AlertIncidentStoragePage> GetOpenPage(AlertIncidentFilter filter, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);
        var rows = Scoped(scope.DbContext, filter.Scope);
        if (filter.ObserverId is not null)
        {
            var observer = filter.ObserverId.Value;
            rows = rows.Where(_ => _.ObserverId == observer);
            if (scope.DbContext.Database.IsSqlServer())
            {
                rows = rows.Where(_ => _.ObserverId + "\u0001" == observer + "\u0001");
            }
        }
        if (filter.Condition is not null)
        {
            var condition = filter.Condition.Value;
            rows = rows.Where(_ => _.Condition == condition);
            if (scope.DbContext.Database.IsSqlServer())
            {
                rows = rows.Where(_ => _.Condition + "\u0001" == condition + "\u0001");
            }
        }
        if (filter.MinimumSeverity is not null)
        {
            var severity = (int)filter.MinimumSeverity.Value;
            rows = rows.Where(_ => _.Severity >= severity);
        }

        return await Page(rows, after, limit, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AlertIncidentCount>> GetOpenCounts(AlertIncidentScope scope, CancellationToken cancellationToken = default)
    {
        await using var databaseScope = await database.Namespace(eventStore, @namespace);

        // Terminated grouping keys prevent SQL Server from merging names that differ by trailing spaces.
        var counts = await Scoped(databaseScope.DbContext, scope)
            .GroupBy(_ => new { Namespace = _.Namespace + "\u0001", Condition = _.Condition + "\u0001", _.Severity })
            .Select(group => new { group.Key.Namespace, group.Key.Condition, group.Key.Severity, Count = group.LongCount() })
            .ToListAsync(cancellationToken);

        return counts.Select(_ => new AlertIncidentCount(_.Namespace[..^1], _.Condition[..^1], (AlertSeverity)_.Severity!.Value, _.Count)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<AlertIncidentStoragePage> EnumerateOpen(AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default)
    {
        await using var scope = await database.Namespace(eventStore, @namespace);

        return await Page(scope.DbContext.AlertIncidents.AsNoTracking().Where(_ => _.IsOpen), after, limit, cancellationToken);
    }

    static IQueryable<AlertIncidentEntity> Scoped(NamespaceDbContext context, AlertIncidentScope scope)
    {
        var store = scope.EventStore.Value;
        var rows = context.AlertIncidents.AsNoTracking().Where(_ => _.IsOpen && _.EventStore == store);

        // SQL Server pads strings for equality even with BIN2. Keep the indexed equality, then
        // append a non-space terminator to enforce ordinal equality including trailing spaces.
        if (context.Database.IsSqlServer())
        {
            rows = rows.Where(_ => _.EventStore + "\u0001" == store + "\u0001");
        }
        if (scope.Namespace is not null)
        {
            var namespaceName = scope.Namespace.Value;
            rows = rows.Where(_ => _.Namespace == namespaceName);
            if (context.Database.IsSqlServer())
            {
                rows = rows.Where(_ => _.Namespace + "\u0001" == namespaceName + "\u0001");
            }
        }

        return rows;
    }

    static async Task<AlertIncidentStoragePage> Page(IQueryable<AlertIncidentEntity> rows, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken)
    {
        limit = AlertIncidentStorageRules.Limit(limit);
        if (after is not null)
        {
            var number = AlertIncidentSequenceNumberConverters.ToSql(after.RaisedSequenceNumber);
            var id = AlertIncidentStorageRules.Key(after.IncidentId);
            rows = rows.Where(_ => _.RaisedSequenceNumber > number || (_.RaisedSequenceNumber == number && _.Id.CompareTo(id) > 0));
        }
        var found = await rows.OrderBy(_ => _.RaisedSequenceNumber).ThenBy(_ => _.Id).Take(limit + 1).ToListAsync(cancellationToken);
        var items = found.Take(limit).Select(_ => _.ToKernel()).ToArray();

        return new(items, found.Count > limit ? new(items[^1].RaisedSequenceNumber!, items[^1].Id) : null);
    }
}
