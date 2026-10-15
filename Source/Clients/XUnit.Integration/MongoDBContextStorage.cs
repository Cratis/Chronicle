// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Owns the MongoDB storage identities issued by one collection fixture.
/// </summary>
/// <param name="createDatabase">Creates a reader for a physical database name.</param>
internal sealed class MongoDBContextStorage(Func<string, MongoDBDatabase> createDatabase)
{
    readonly HashSet<string> _prefixes = [];
    MongoDBDatabase? _eventStore;
    MongoDBDatabase? _eventStoreForNamespace;
    MongoDBDatabase? _readModels;

    /// <summary>
    /// Gets the database name prefix of the current context.
    /// </summary>
    internal string Prefix { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the reader for the current context's event store database.
    /// </summary>
    internal MongoDBDatabase EventStore => _eventStore ??= createDatabase($"{Prefix}{Constants.EventStoreDatabaseName}");

    /// <summary>
    /// Gets the reader for the current context's default namespace database.
    /// </summary>
    internal MongoDBDatabase EventStoreForNamespace => _eventStoreForNamespace ??= createDatabase($"{Prefix}{Constants.EventStoreNamespaceDatabaseName}");

    /// <summary>
    /// Gets the reader for the current context's read models database.
    /// </summary>
    internal MongoDBDatabase ReadModels => _readModels ??= createDatabase($"{Prefix}{Constants.ReadModelsDatabaseName}");

    /// <summary>
    /// Begins a new context with a prefix that has never been issued by this instance.
    /// </summary>
    /// <returns>The prefix of the new context.</returns>
    internal string BeginContext()
    {
        ResetReaders();

        // The longest logical database name is 52 bytes, leaving at most 11 bytes for its prefix.
        // Each collection fixture owns its MongoDB container and this registry for that container's lifetime.
        // A checked monotonic counter never reuses an identity; hexadecimal keeps even int.MaxValue within 10 bytes.
        var prefix = $"c{checked(_prefixes.Count + 1):x}_";
        _prefixes.Add(prefix);

        Prefix = prefix;
        return prefix;
    }

    /// <summary>
    /// Drops every database whose name starts with a prefix issued by this instance.
    /// </summary>
    /// <param name="client">The <see cref="IMongoClient"/> to drop the databases with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task DropDatabases(IMongoClient client)
    {
        using var cursor = await client.ListDatabaseNamesAsync();
        var names = await cursor.ToListAsync();
        foreach (var name in names.Where(name => _prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))))
        {
            await client.DropDatabaseAsync(name);
        }
    }

    /// <summary>
    /// Releases the readers of the current context so the next access creates readers for the current prefix.
    /// </summary>
    internal void ResetReaders()
    {
        _eventStore?.Dispose();
        _eventStoreForNamespace?.Dispose();
        _readModels?.Dispose();
        _eventStore = null;
        _eventStoreForNamespace = null;
        _readModels = null;
    }
}
