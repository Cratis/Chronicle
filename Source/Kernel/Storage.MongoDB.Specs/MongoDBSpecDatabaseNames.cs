// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Provides a process-wide database prefix shared by the storage spec fixtures.
/// </summary>
public static class MongoDBSpecDatabaseNames
{
    /// <summary>
    /// Gets the external connection string, if configured.
    /// </summary>
    public static string? ExternalConnectionString { get; } =
        Environment.GetEnvironmentVariable("CHRONICLE_MONGODB_CONNECTION_DETAILS") is { Length: > 0 } value ? value : null;

    /// <summary>
    /// Gets the unique prefix for this test run.
    /// </summary>
    public static string Prefix { get; } = $"chr_{Guid.NewGuid():N}_";

    /// <summary>
    /// Creates a unique database name within MongoDB's 63-byte limit.
    /// </summary>
    /// <returns>A database name scoped to this run.</returns>
    public static string New() => $"{Prefix}{Guid.NewGuid().ToString("N")[..16]}";
}
