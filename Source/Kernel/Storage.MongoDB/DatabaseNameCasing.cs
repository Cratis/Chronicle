// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Resolves the physical name of a database against the databases a server already has, ignoring case.
/// </summary>
/// <remarks>
/// MongoDB refuses to create a database whose name differs only in case from one that exists ("db already exists with
/// different case"). Chronicle's logical names keep the casing they were given, so a namespace stored as 'cratis'
/// next to an existing 'Studio+es+Cratis' database would otherwise stop the server the first time it writes there.
/// When a case variant of the requested name exists, its existing casing is used; otherwise the requested name is.
/// Known names are cached per client, so only a name not seen before lists the server's databases. The first casing
/// claimed for a name wins, so two case variants requested at once open the same database.
/// </remarks>
internal static class DatabaseNameCasing
{
    static readonly ConditionalWeakTable<IMongoClient, ServerDatabaseNames> _servers = [];

    /// <summary>
    /// Gets a database by the casing it already has on the server of the client, or by the requested name when none does.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> of the server the database is on.</param>
    /// <param name="name">The requested database name.</param>
    /// <param name="logger">Optional <see cref="ILogger"/> for when the server's databases cannot be listed.</param>
    /// <returns>The <see cref="IMongoDatabase"/>.</returns>
    internal static IMongoDatabase GetDatabaseByExistingCasing(this IMongoClient client, string name, ILogger? logger = null) =>
        client.GetDatabase(Resolve(client, name, logger));

    /// <summary>
    /// Resolves the physical name of a database on the server of a client.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> of the server the database is on.</param>
    /// <param name="name">The requested database name.</param>
    /// <param name="logger">Optional <see cref="ILogger"/> for when the server's databases cannot be listed.</param>
    /// <returns>The existing casing of the name if a case variant exists; otherwise the requested name.</returns>
    internal static string Resolve(IMongoClient client, string name, ILogger? logger = null) =>
        _servers.GetValue(client, static _ => new()).Resolve(client, name, logger);

    sealed class ServerDatabaseNames
    {
        readonly ConcurrentDictionary<string, string> _names = new(StringComparer.OrdinalIgnoreCase);
        readonly Lock _refreshLock = new();

        public string Resolve(IMongoClient client, string name, ILogger? logger)
        {
            if (_names.TryGetValue(name, out var existing))
            {
                return existing;
            }

            lock (_refreshLock)
            {
                if (_names.TryGetValue(name, out existing))
                {
                    return existing;
                }

                try
                {
                    foreach (var databaseName in client.ListDatabaseNames().ToList())
                    {
                        _names.TryAdd(databaseName, databaseName);
                    }
                }
                catch (Exception ex)
                {
                    // Without the right to list databases the requested name is used as it is, as before.
                    logger?.CouldNotListDatabases(name, ex);
                }

                // The name is claimed even when it does not exist yet, so a case variant of it asked for later opens
                // the same database instead of failing to create a second one.
                return _names.GetOrAdd(name, name);
            }
        }
    }
}
