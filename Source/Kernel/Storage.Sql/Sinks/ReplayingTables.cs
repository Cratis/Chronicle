// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Knows which read model tables a replay is rebuilding, for every SQL <see cref="Sink"/> in the silo.
/// </summary>
/// <remarks>
/// The kernel does not reach a read model through one sink instance. The replay handler, the projection pipeline
/// and the read model store each ask for a sink, and a projection pipeline keeps the sink it was built with after
/// it has been evicted. Whether writes go to the replay table is therefore a fact about the table, not about the
/// sink instance the replay happened to begin or end on. Kept per instance, a pipeline built during a replay went on
/// writing to a replay table nothing would swap in again, and one built before it wrote around the replay into the
/// table the replay then replaced.
/// </remarks>
public class ReplayingTables
{
    readonly ConcurrentDictionary<TableKey, byte> _tables = new();

    /// <summary>
    /// Marks a table as being rebuilt by a replay.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the table belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the table belongs to.</param>
    /// <param name="table">The name of the read model table.</param>
    public void Begin(EventStoreName eventStore, EventStoreNamespaceName @namespace, string table) =>
        _tables[KeyFor(eventStore, @namespace, table)] = 0;

    /// <summary>
    /// Marks a table as no longer being rebuilt by a replay.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the table belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the table belongs to.</param>
    /// <param name="table">The name of the read model table.</param>
    public void End(EventStoreName eventStore, EventStoreNamespaceName @namespace, string table) =>
        _tables.TryRemove(KeyFor(eventStore, @namespace, table), out _);

    /// <summary>
    /// Checks whether a replay is rebuilding a table.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the table belongs to.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the table belongs to.</param>
    /// <param name="table">The name of the read model table.</param>
    /// <returns>True if a replay is rebuilding the table, false if not.</returns>
    public bool IsReplaying(EventStoreName eventStore, EventStoreNamespaceName @namespace, string table) =>
        _tables.ContainsKey(KeyFor(eventStore, @namespace, table));

    static TableKey KeyFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, string table) =>
        new(eventStore.Value, @namespace.Value, table);

    sealed record TableKey(string EventStore, string Namespace, string Table);
}
