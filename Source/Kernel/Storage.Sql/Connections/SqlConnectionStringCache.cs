// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Cluster;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Keeps the latest normalization for each logical database and purpose, replacing rotated credentials rather than retaining their history.
/// </summary>
internal sealed class SqlConnectionStringCache
{
    readonly ConcurrentDictionary<(DatabaseIdentity Identity, ConnectionPurpose Purpose), Normalization> _normalizations = new();

    /// <summary>
    /// Normalize only when a string-affecting input changes. Resolution and identity refresh remain outside this cache.
    /// </summary>
    /// <param name="identity">The logical database.</param>
    /// <param name="purpose">The connection purpose.</param>
    /// <param name="input">All inputs to the normalization.</param>
    /// <returns>The normalized connection string.</returns>
    internal string Get(DatabaseIdentity identity, ConnectionPurpose purpose, SqlConnectionStringInput input) =>
        _normalizations.GetOrAdd((identity, purpose), static _ => new()).Get(input);

    sealed class Normalization
    {
        readonly Lock _lock = new();
        Entry? _entry;

        internal string Get(SqlConnectionStringInput input)
        {
            var cached = Volatile.Read(ref _entry);
            if (cached?.Input == input)
            {
                return cached.ConnectionString;
            }

            lock (_lock)
            {
                cached = _entry;
                if (cached?.Input == input)
                {
                    return cached.ConnectionString;
                }

                var updated = new Entry(input, input.Normalize());
                Volatile.Write(ref _entry, updated);
                return updated.ConnectionString;
            }
        }
    }

    sealed record Entry(SqlConnectionStringInput Input, string ConnectionString);
}
