// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the MongoDB implementation of <see cref="IResourcePhysicalNames"/>.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> holding the cluster database.</param>
/// <param name="options">The <see cref="MongoDBStorageOptions"/>.</param>
public class MongoDBResourcePhysicalNames(IDatabase database, IOptions<MongoDBStorageOptions> options) : IResourcePhysicalNames
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> For(EventStoreName eventStore, EventStoreNamespaceName? @namespace)
    {
        var prefix = options.Value.DatabaseNamePrefix;
        var systemDatabaseName = options.Value.SystemDatabaseName;
        if (@namespace is null)
        {
            return [DatabaseNames.ComposeForEventStore(eventStore, prefix, systemDatabaseName)];
        }

        return
        [
            DatabaseNames.ComposeForEventStoreNamespace(eventStore, @namespace, prefix, systemDatabaseName),
            DatabaseNames.ComposeForReadModels(eventStore, @namespace, prefix, systemDatabaseName)
        ];
    }

    /// <inheritdoc/>
    /// <remarks>
    /// MongoDB refuses a database whose name differs from an existing one only in case and compares names
    /// byte-exactly otherwise, so a case-insensitive ordinal match is its equality or a superset of it.
    /// </remarks>
    public async Task<bool> AnyExists(IReadOnlyCollection<string> names)
    {
        var client = database.GetCollection<BsonDocument>(WellKnownCollectionNames.ResourceNameReservations).Database.Client;
        using var cursor = await client.ListDatabaseNamesAsync();
        var existing = new HashSet<string>(await cursor.ToListAsync(), StringComparer.OrdinalIgnoreCase);
        return names.Any(existing.Contains);
    }
}
