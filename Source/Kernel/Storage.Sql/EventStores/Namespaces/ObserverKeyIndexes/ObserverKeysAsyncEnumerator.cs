// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes;

/// <summary>
/// Represents an implementation of <see cref="IAsyncEnumerator{T}"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="eventSequenceId">The event sequence identifier.</param>
/// <param name="fromEventSequenceNumber">The <see cref="EventSequenceNumber"/> to start from.</param>
/// <param name="eventTypes">Collection of <see cref="EventTypeId"/> the index is for.</param>
/// <param name="cancellationToken">The cancellation token.</param>
public class ObserverKeysAsyncEnumerator(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database,
    EventSequenceId eventSequenceId,
    EventSequenceNumber fromEventSequenceNumber,
    IEnumerable<EventTypeId> eventTypes,
    CancellationToken cancellationToken) : IAsyncEnumerator<Key>
{
    readonly string[] _eventTypeStringIds = eventTypes.Select(et => et.Value).ToArray();
    List<EventSourceId>? _results;
    int _currentIndex = -1;
    DbContextScope<EventSequences.EventSequenceDbContext>? _scope;
    Key? _current;

    /// <inheritdoc/>
    public Key Current => _current!;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_scope is not null)
        {
            await _scope.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (_results is null)
        {
            if (_eventTypeStringIds.Length == 0)
            {
                _results = [];
                _current = null;
                return false;
            }

            _scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

            var fromSeqValue = fromEventSequenceNumber.Value;

            // Fetch event source IDs and types from the database using only the sequence number
            // filter in SQL. The event type filter is applied in memory to avoid EF Core query
            // translation issues with ConceptAs value-converted types in Contains expressions.
            var entries = await _scope.DbContext.Events
                .Where(e => e.SequenceNumber >= fromSeqValue)
                .Select(e => new { e.EventSourceId, e.Type })
                .ToListAsync(cancellationToken);

            _results = entries
                .Where(e => _eventTypeStringIds.Contains(e.Type.Value))
                .Select(e => e.EventSourceId)
                .Distinct()
                .ToList();
        }

        _currentIndex++;
        if (_currentIndex < _results.Count)
        {
            _current = new Key(_results[_currentIndex].Value, ArrayIndexers.NoIndexers);
            return true;
        }

        _current = null;
        return false;
    }
}
