// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Storage.Alerts;

namespace Cratis.Chronicle.Storage.InMemory.Alerts;

/// <summary>
/// Provides namespace-owned atomic in-memory incident storage.
/// </summary>
public class AlertIncidentsStorage : IAlertIncidentsStorage
{
    readonly Lock _lock = new();
    readonly Dictionary<IncidentId, AlertIncident> _incidents = new();

    /// <inheritdoc/>
    public Task<AlertIncidentWriteOutcome> Apply(AlertIncidentTransition transition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AlertIncidentStorageRules.Validate(transition.SequenceNumber);
        lock (_lock)
        {
            _incidents.TryGetValue(transition.Id, out var current);
            var result = AlertIncidentFold.Apply(current, transition);
            if (result.Outcome == AlertIncidentWriteOutcome.Applied)
            {
                _incidents[transition.Id] = result.Incident!;
            }

            return Task.FromResult(result.Outcome);
        }
    }

    /// <inheritdoc/>
    public Task<AlertIncident?> GetOpen(AlertIncidentScope scope, IncidentId incidentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(Open(scope).FirstOrDefault(row => row.Id == incidentId));
        }
    }

    /// <inheritdoc/>
    public Task<AlertIncidentStoragePage> GetOpenPage(AlertIncidentFilter filter, AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var rows = Open(filter.Scope).Where(row =>
                (filter.ObserverId is null || row.Target.ObserverId == filter.ObserverId) &&
                (filter.Condition is null || row.Condition == filter.Condition) &&
                (filter.MinimumSeverity is null || row.Severity >= filter.MinimumSeverity));

            return Task.FromResult(Page(rows, after, limit));
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<AlertIncidentCount>> GetOpenCounts(AlertIncidentScope scope, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var counts = Open(scope).GroupBy(row => (row.Target.Namespace, row.Condition, Severity: row.Severity!.Value))
                .Select(group => new AlertIncidentCount(group.Key.Namespace, group.Key.Condition, group.Key.Severity, group.LongCount())).ToArray();

            return Task.FromResult<IEnumerable<AlertIncidentCount>>(counts);
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<AlertIncidentObserverCount>> GetOpenCountsByObserver(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var counts = _incidents.Values.Where(row => row.IsOpen)
                .GroupBy(row => (row.Target.EventStore, row.Target.Namespace, row.Target.ObserverId, row.Target.EventSequenceId, row.Condition, Severity: row.Severity!.Value))
                .Select(group => new AlertIncidentObserverCount(group.Key.EventStore, group.Key.Namespace, group.Key.ObserverId, group.Key.EventSequenceId, group.Key.Condition, group.Key.Severity, group.LongCount())).ToArray();

            return Task.FromResult<IEnumerable<AlertIncidentObserverCount>>(counts);
        }
    }

    /// <inheritdoc/>
    public Task<AlertIncidentStoragePage> EnumerateOpen(AlertIncidentCursor? after, int limit, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(Page(_incidents.Values.Where(row => row.IsOpen), after, limit));
        }
    }

    static AlertIncidentStoragePage Page(IEnumerable<AlertIncident> rows, AlertIncidentCursor? after, int limit)
    {
        limit = AlertIncidentStorageRules.Limit(limit);
        if (after is not null)
        {
            AlertIncidentStorageRules.Validate(after.RaisedSequenceNumber);
            rows = rows.Where(row => row.RaisedSequenceNumber! > after.RaisedSequenceNumber ||
                (row.RaisedSequenceNumber == after.RaisedSequenceNumber &&
                 string.CompareOrdinal(AlertIncidentStorageRules.Key(row.Id), AlertIncidentStorageRules.Key(after.IncidentId)) > 0));
        }
        var found = rows.OrderBy(row => row.RaisedSequenceNumber!.Value)
            .ThenBy(row => AlertIncidentStorageRules.Key(row.Id), StringComparer.Ordinal).Take(limit + 1).ToArray();
        var items = found.Take(limit).ToArray();

        return new(items, found.Length > limit ? new(items[^1].RaisedSequenceNumber!, items[^1].Id) : null);
    }

    IEnumerable<AlertIncident> Open(AlertIncidentScope scope) => _incidents.Values.Where(row => row.IsOpen &&
        row.Target.EventStore == scope.EventStore && (scope.Namespace is null || row.Target.Namespace == scope.Namespace));
}
