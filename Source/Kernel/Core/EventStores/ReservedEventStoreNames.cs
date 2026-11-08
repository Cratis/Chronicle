// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Holds the rule for the event store names that may not be used for a new event store.
/// </summary>
/// <remarks>
/// The checks run on every database resolution, so they compare spans and never allocate.
/// </remarks>
public static class ReservedEventStoreNames
{
    /// <summary>
    /// Check whether an event store name is not allowed, because the databases it resolves to could be, or could be
    /// made to alias, the System, cluster or database-internal databases.
    /// </summary>
    /// <param name="name">The event store name.</param>
    /// <param name="systemDatabaseName">The name the System event store's databases are named by.</param>
    /// <returns>True when the name is not allowed.</returns>
    /// <remarks>
    /// The <see cref="EventStoreName.System"/> event store itself is not covered, it is the one event store that is
    /// allowed to resolve to the System databases.
    /// </remarks>
    public static bool IsReserved(string name, string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName) =>
        name.Contains('+') ||
        string.Equals(name, systemDatabaseName, StringComparison.OrdinalIgnoreCase) ||
        IsInternalDatabaseName(name);

    /// <summary>
    /// Check whether a name is one of the databases the database server keeps for itself.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>True when the name is reserved by the database server.</returns>
    public static bool IsInternalDatabaseName(string name) =>
        string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "local", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(name, "config", StringComparison.OrdinalIgnoreCase);
}
