// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Neo4jSinkProvenanceStatements;

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Stores a monotonic provenance marker in the Runtime graph database, outside application labels.
/// </summary>
/// <param name="target">The graph route shared with the payload sink.</param>
internal sealed class Neo4jSinkProvenanceStorage(Neo4jGraphTarget target) : ISinkProvenanceStorage, IDisposable
{
    readonly SemaphoreSlim _schema = new(1, 1);
    bool _schemaReady;

    /// <summary>
    /// Gets the route shared with the payload sink.
    /// </summary>
    internal Neo4jGraphTarget Target => target;

    /// <inheritdoc/>
    public Task<SinkProvenanceState> ReadState() => Execute(GetOrSeed);

    /// <inheritdoc/>
    public Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence) => Execute(async transaction =>
    {
        await GetOrSeed(transaction);
        await Run(transaction, LockInventory);
        var state = await RequireState(transaction);
        if (state.IsComplete) return state;

        var restricted = restrictedEvidence || state.Restricted;
        if (restricted) await Run(transaction, Mark);
        await Run(transaction, Complete, new { version = SinkProvenanceState.CurrentInventoryVersion, restricted });
        return await RequireState(transaction);
    });

    /// <inheritdoc/>
    public async Task MarkQuarantined() => await Execute(async transaction =>
    {
        await GetOrSeed(transaction);
        await Run(transaction, LockInventory);
        await Run(transaction, Mark);
        await Run(transaction, LatchRestriction);
        var state = await RequireState(transaction);
        return state.Restricted ? state : throw Neo4jGraphTarget.Unavailable();
    });

    /// <inheritdoc/>
    public async Task<bool> IsQuarantined() => (await ReadState()).Restricted;

    /// <inheritdoc/>
    public void Dispose() => _schema.Dispose();

    static async Task<SinkProvenanceState> GetOrSeed(IAsyncQueryRunner transaction)
    {
        var state = await ReadState(transaction);
        if (state is not null) return state;

        // Missing is unknown. MERGE is guarded by the verified unique key; read back the winning GUID.
        await Run(transaction, Seed, new { id = Guid.NewGuid().ToString("D") });
        return await RequireState(transaction);
    }

    static async Task<SinkProvenanceState?> ReadState(IAsyncQueryRunner transaction)
    {
        var cursor = await transaction.RunAsync(Read);
        SinkProvenanceState? state = null;
        var marked = false;
        while (await cursor.FetchAsync())
        {
            var fields = Fields(cursor.Current);
            switch (Get<string>(fields, "_key"))
            {
                case "inventory" when state is null:
                    state = ParseInventory(fields);
                    break;
                case "protected" when !marked && fields.Count == 2:
                    marked = true;
                    break;
                default:
                    throw Neo4jGraphTarget.Unavailable();
            }
        }

        return state is not null && marked ? state with { Restricted = true } : state;
    }

    static async Task<SinkProvenanceState> RequireState(IAsyncQueryRunner transaction) =>
        await ReadState(transaction) ?? throw Neo4jGraphTarget.Unavailable();

    static async Task Run(IAsyncQueryRunner transaction, string statement, object? parameters = null)
    {
        var cursor = await transaction.RunAsync(statement, parameters);
        await cursor.ConsumeAsync();
    }

    async Task<SinkProvenanceState> Execute(Func<IAsyncQueryRunner, Task<SinkProvenanceState>> action)
    {
        var runtime = await target.Runtime();
        var schema = await target.Schema(runtime);
        await EnsureSchema(runtime, schema);
        await using var session = runtime.OpenSession();
        return await session.ExecuteWriteAsync(async transaction =>
        {
            await target.EnsureDatabase(transaction);
            return await action(transaction);
        });
    }

    async Task EnsureSchema(Neo4jConnection runtime, Neo4jConnection schema)
    {
        await _schema.WaitAsync();
        try
        {
            if (_schemaReady) return;
            await target.PrepareSchema(runtime, schema);
            await using var session = schema.OpenSession();
            await session.ExecuteWriteAsync(async transaction =>
            {
                await target.EnsureDatabase(transaction);
                await Run(transaction, CreateConstraint);
            });
            await session.ExecuteWriteAsync(async transaction =>
            {
                await target.EnsureDatabase(transaction);
                var cursor = await transaction.RunAsync(VerifyConstraint);
                if (!await cursor.FetchAsync() || await cursor.FetchAsync()) throw Neo4jGraphTarget.Unavailable();
            });
            _schemaReady = true;
        }
        finally
        {
            _schema.Release();
        }
    }
}
