// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverKeyIndexes;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndexes"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class ObserverKeyIndexes(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database) : IObserverKeyIndexes
{
    /// <inheritdoc/>
    public Task<IObserverKeyIndex> GetFor(ObserverKey observerKey, ObserverReaderScope scope) =>
        Task.FromResult<IObserverKeyIndex>(new EventSourceKeyIndex(eventStore, @namespace, database, observerKey.EventSequenceId, scope));
}
