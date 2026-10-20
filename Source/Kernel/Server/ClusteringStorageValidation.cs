// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Validates that cluster membership can use the configured storage backend.
/// </summary>
internal static class ClusteringStorageValidation
{
    /// <summary>
    /// Checks the explicitly selected clustering type before configuring the silo.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <exception cref="SqlClusteringRequiresServerStorage">SQL clustering needs SQL Server or PostgreSQL storage.</exception>
    /// <exception cref="MongoDBClusteringRequiresMongoDBStorage">MongoDB clustering needs MongoDB storage.</exception>
    internal static void Validate(ChronicleOptions options)
    {
        var storage = options.Storage.Type;
        if (options.Clustering.Type == ClusteringType.Sql
            && !string.Equals(storage, StorageType.MsSql, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(storage, StorageType.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            throw new SqlClusteringRequiresServerStorage(storage);
        }

        // The server falls back to MongoDB storage when no type is set, so only reject storage that is
        // explicitly something else.
        if (options.Clustering.Type == ClusteringType.MongoDB && IsExplicitlyNotMongoDB(storage))
        {
            throw new MongoDBClusteringRequiresMongoDBStorage(storage);
        }
    }

    static bool IsExplicitlyNotMongoDB(string storage) =>
        new[] { StorageType.InMemory, StorageType.Sqlite, StorageType.MsSql, StorageType.PostgreSql }
            .Any(type => string.Equals(storage, type, StringComparison.OrdinalIgnoreCase));
}
