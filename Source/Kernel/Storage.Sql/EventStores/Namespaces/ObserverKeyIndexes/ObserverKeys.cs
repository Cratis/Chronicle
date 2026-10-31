// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeys"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="eventSequenceId">The event sequence identifier.</param>
/// <param name="fromEventSequenceNumber">The <see cref="EventSequenceNumber"/> we want to get keys starting from.</param>
/// <param name="scope">The <see cref="ObserverReaderScope"/> the index is for.</param>
/// <remarks>
/// A wildcard scope enumerates every key without an event-type filter, matching MongoDB.
/// An explicit scope narrows by its types; an empty explicit scope enumerates no keys.
/// </remarks>
public class ObserverKeys(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database,
    EventSequenceId eventSequenceId,
    EventSequenceNumber fromEventSequenceNumber,
    ObserverReaderScope scope) : IObserverKeys
{
    /// <inheritdoc/>
    public IAsyncEnumerator<Key> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new ObserverKeysAsyncEnumerator(eventStore, @namespace, database, eventSequenceId, fromEventSequenceNumber, scope, cancellationToken);
}
