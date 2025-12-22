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
    readonly string[] _eventTypeIds = eventTypes.Select(eventType => eventType.Value).ToArray();
    IAsyncEnumerator<string>? _enumerator;
    Key? _current;

    /// <inheritdoc/>
    public Key Current => _current!;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_enumerator is not null)
        {
            await _enumerator.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    public async ValueTask<bool> MoveNextAsync()
    {
        if (_enumerator is null)
        {
            await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);

            var query = scope.DbContext.Events
                .Where(e => e.SequenceNumber >= fromEventSequenceNumber &&
                           _eventTypeIds.Contains(e.Type))
                .Select(e => e.EventSourceId)
                .Distinct();

            _enumerator = query.AsAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
        }

        var hasNext = await _enumerator.MoveNextAsync();
        if (hasNext)
        {
            _current = new Key(_enumerator.Current, ArrayIndexers.NoIndexers);
            return true;
        }

        _current = null;
        return false;
    }
}
