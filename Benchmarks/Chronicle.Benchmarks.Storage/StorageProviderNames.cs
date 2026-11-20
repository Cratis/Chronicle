// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Parses provider names used on the command line.</summary>
public static class StorageProviderNames
{
    /// <summary>Gets the command line name of a provider.</summary>
    /// <param name="provider">The provider.</param>
    /// <returns>The lower-case name used on the command line and in result folders.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The provider is not known.</exception>
    public static string NameOf(StorageProvider provider) => provider switch
    {
        StorageProvider.Mongo => "mongo",
        StorageProvider.PostgreSql => "postgres",
        StorageProvider.SqlServer => "sqlserver",
        StorageProvider.Sqlite => "sqlite",
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    /// <summary>Parses a comma separated provider list.</summary>
    /// <param name="value">The list, for example `sqlite,postgres,mongo`.</param>
    /// <returns>The providers, in the order given, without duplicates.</returns>
    /// <exception cref="ArgumentException">A name is not a known provider.</exception>
    public static IReadOnlyList<StorageProvider> Parse(string value)
    {
        var providers = new List<StorageProvider>();
        foreach (var name in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var provider = ParseOne(name);
            if (!providers.Contains(provider)) providers.Add(provider);
        }

        return providers;
    }

    static StorageProvider ParseOne(string name) => name.ToLowerInvariant() switch
    {
        "mongo" or "mongodb" => StorageProvider.Mongo,
        "postgres" or "postgresql" or "pg" => StorageProvider.PostgreSql,
        "sqlserver" or "mssql" => StorageProvider.SqlServer,
        "sqlite" => StorageProvider.Sqlite,
        _ => throw new ArgumentException($"Unknown storage provider '{name}'. Use mongo, postgres, sqlserver or sqlite.", nameof(name))
    };
}
