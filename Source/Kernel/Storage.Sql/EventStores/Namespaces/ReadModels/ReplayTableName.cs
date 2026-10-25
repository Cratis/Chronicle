// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// The naming of the shadow table a replay rebuilds a read model in.
/// </summary>
internal static class ReplayTableName
{
    /// <summary>
    /// The prefix of every replay table.
    /// </summary>
    internal const string Prefix = "replay-";

    /// <summary>
    /// Gets the name of the replay table for a read model table.
    /// </summary>
    /// <param name="baseName">The read model table name.</param>
    /// <returns>The replay table name.</returns>
    internal static string For(string baseName) => $"{Prefix}{baseName}";

    /// <summary>
    /// Gets whether a table name is a replay table name.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <returns>True when it is a replay table.</returns>
    internal static bool IsReplayTable(string tableName) => tableName.StartsWith(Prefix, StringComparison.Ordinal);
}
