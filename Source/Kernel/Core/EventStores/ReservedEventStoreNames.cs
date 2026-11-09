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
    /// <param name="prefix">The configured database name prefix; the name is also not allowed when prefix and name together make a database-internal name.</param>
    /// <returns>True when the name is not allowed.</returns>
    /// <remarks>
    /// The <see cref="EventStoreName.System"/> event store itself is not covered, it is the one event store that is
    /// allowed to resolve to the System databases.
    /// </remarks>
    public static bool IsReserved(string name, string systemDatabaseName = Configuration.Storage.DefaultSystemDatabaseName, string prefix = "") =>
        name.Contains('+') ||
        string.Equals(name, systemDatabaseName, StringComparison.OrdinalIgnoreCase) ||
        IsInternalDatabaseName(name) ||
        AliasesInternalDatabaseName(prefix, name);

    /// <summary>
    /// Check whether a database name prefix followed by a name makes one of the databases the database server keeps for itself.
    /// </summary>
    /// <param name="prefix">The configured database name prefix.</param>
    /// <param name="name">The name that follows the prefix.</param>
    /// <returns>True when the two together are a reserved name. Never allocates.</returns>
    public static bool AliasesInternalDatabaseName(string prefix, string name)
    {
        var length = prefix.Length + name.Length;
        if (prefix.Length == 0 || length is < 5 or > 6)
        {
            return false;
        }

        Span<char> composed = stackalloc char[6];
        prefix.CopyTo(composed);
        name.CopyTo(composed[prefix.Length..]);
        var value = composed[..length];
        return value.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("local", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("config", StringComparison.OrdinalIgnoreCase);
    }

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
