// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Neo4j.Driver;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Pins a graph route to one database across provenance, payload and schema transactions.
/// </summary>
internal sealed class Neo4jGraphTarget(INeo4jConnections connections, EventStoreName store, EventStoreNamespaceName scope)
{
    readonly Lock _gate = new();
    Neo4jConnection? _placement;
    string? _databaseId;

    internal static InvalidOperationException Unavailable() => new("Read-model storage is not available.");

    internal async Task<Neo4jConnection> Runtime()
    {
        var connection = await connections.Resolve(store, scope, ConnectionPurpose.Runtime);
        if (string.IsNullOrWhiteSpace(connection.Database) || string.Equals(connection.Database, Neo4jConnection.SystemDatabase, StringComparison.OrdinalIgnoreCase)) throw Unavailable();
        lock (_gate)
        {
            _placement ??= connection;
            EnsureSamePlacement(_placement, connection);
        }
        return connection;
    }

    internal async Task<Neo4jConnection> Schema(Neo4jConnection runtime)
    {
        var schema = await connections.Resolve(store, scope, ConnectionPurpose.Schema);
        EnsureSamePlacement(runtime, schema);
        return schema;
    }

    internal async Task PrepareSchema(Neo4jConnection runtime, Neo4jConnection schema)
    {
        // Only the configured, Chronicle-owned placement may be created. Never provision an alternate Schema target.
        if (MayProvision(runtime, schema))
        {
            await using var session = schema.OpenSystemSession();
            var cursor = await session.RunAsync($"CREATE DATABASE {GraphCypher.Escape(runtime.Database)} IF NOT EXISTS WAIT");
            await cursor.ConsumeAsync();
        }

        // Establish the physical identity under Runtime credentials before any Schema transaction can mutate it.
        await using var runtimeSession = runtime.OpenSession();
        await runtimeSession.ExecuteWriteAsync(EnsureDatabase);
    }

    internal async Task EnsureDatabase(IAsyncQueryRunner transaction)
    {
        var cursor = await transaction.RunAsync("CALL db.info() YIELD id RETURN id");
        if (!await cursor.FetchAsync()) throw Unavailable();
        if (cursor.Current["id"] is not string { Length: > 0 } databaseId) throw Unavailable();
        if (await cursor.FetchAsync()) throw Unavailable();
        lock (_gate)
        {
            _databaseId ??= databaseId;
            if (_databaseId != databaseId) throw Unavailable();
        }
    }

    bool MayProvision(Neo4jConnection runtime, Neo4jConnection schema) =>
        runtime.CanCreateDatabase && schema.CanCreateDatabase && Volatile.Read(ref _databaseId) is null;

    static void EnsureSamePlacement(Neo4jConnection expected, Neo4jConnection actual)
    {
        var sameEndpoint = expected.Endpoint is null || actual.Endpoint is null
            ? ReferenceEquals(expected.Driver, actual.Driver)
            : expected.Endpoint == actual.Endpoint;
        if (!sameEndpoint || !string.Equals(expected.Database, actual.Database, StringComparison.OrdinalIgnoreCase)) throw Unavailable();
    }
}
