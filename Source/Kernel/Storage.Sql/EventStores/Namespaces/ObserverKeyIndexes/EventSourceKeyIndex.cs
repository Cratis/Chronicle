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
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="eventSequenceId">The event sequence identifier.</param>
/// <param name="scope">The <see cref="ObserverReaderScope"/> the index is for.</param>
public class EventSourceKeyIndex(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database,
    EventSequenceId eventSequenceId,
    ObserverReaderScope scope) : IObserverKeyIndex
{
    /// <inheritdoc/>
    public Task Add(Key key) => Task.CompletedTask;

    /// <inheritdoc/>
    public IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber) =>
       new ObserverKeys(eventStore, @namespace, database, eventSequenceId, fromEventSequenceNumber, scope);

    /// <inheritdoc/>
    public Task Rebuild() => Task.CompletedTask;
}
