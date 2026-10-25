// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Reports or refuses dynamic DDL after callers have determined that a change is needed.
/// </summary>
/// <param name="logger">The schema logger.</param>
internal sealed partial class DynamicSchemaGuard(ILogger<DynamicSchemaGuard> logger)
{
    static readonly AsyncLocal<DynamicSchemaPolicy?> _override = new();
    static readonly DynamicSchemaGuard _default = new(NullLogger<DynamicSchemaGuard>.Instance);
    readonly ConcurrentDictionary<(string Connection, string Kind, string Name), byte> _reported = new();
    readonly AsyncLocal<int> _batchDepth = new();
    int _pending;

    /// <summary>
    /// Gets whether an explicit provisioning scope is active.
    /// </summary>
    internal static bool IsProvisioning => _override.Value == DynamicSchemaPolicy.Allow;

    /// <summary>
    /// Finds the process guard carried by the context options.
    /// </summary>
    /// <param name="context">The connected context.</param>
    /// <returns>The shared guard.</returns>
    internal static DynamicSchemaGuard For(DbContext context) =>
        context.GetService<IDbContextOptions>().FindExtension<SchemaModeOptionsExtension>()?.Guard ?? _default;

    /// <summary>
    /// Resolves the policy without querying the database.
    /// </summary>
    /// <param name="context">The context options carrying the schema mode.</param>
    /// <returns>The effective policy.</returns>
    internal static DynamicSchemaPolicy PolicyFor(DbContext context) => _override.Value ??
        (context.GetService<IDbContextOptions>().FindExtension<SchemaModeOptionsExtension>()?.Mode == SchemaMode.Verify
            ? DynamicSchemaPolicy.Warn : DynamicSchemaPolicy.Allow);

    /// <summary>
    /// Overrides policy for explicit provisioning or an internal specification.
    /// </summary>
    /// <param name="policy">The policy, which has no public configuration switch.</param>
    /// <returns>A scope restoring the previous asynchronous-flow policy.</returns>
    internal static IDisposable Use(DynamicSchemaPolicy policy)
    {
        var previous = _override.Value;
        _override.Value = policy;
        return new Scope(() => _override.Value = previous);
    }

    /// <summary>
    /// Checks a schema change, without reading the catalog or executing DDL.
    /// </summary>
    /// <param name="context">The context whose database will change.</param>
    /// <param name="kind">The object kind.</param>
    /// <param name="name">The object name.</param>
    /// <exception cref="DynamicSchemaObjectNotProvisioned">The internal refusal policy rejects the change.</exception>
    internal void Require(DbContext context, string kind, string name)
    {
        var policy = PolicyFor(context);
        if (policy == DynamicSchemaPolicy.Allow) return;
        var database = context.Database.GetDbConnection().Database;
        if (policy == DynamicSchemaPolicy.Refuse) throw new DynamicSchemaObjectNotProvisioned(kind, name, database);
        if (_reported.TryAdd((context.Database.GetConnectionString() ?? string.Empty, kind, name), 0))
        {
            RuntimeSchemaObject(kind, name, database);
            Interlocked.Increment(ref _pending);
        }
    }

    /// <summary>
    /// Flushes newly reported objects as one warning when the DDL batch finishes.
    /// </summary>
    /// <returns>A scope flushing on success or failure.</returns>
    internal IDisposable FlushOnDispose()
    {
        var previous = _batchDepth.Value;
        _batchDepth.Value = previous + 1;
        return new Scope(() =>
        {
            _batchDepth.Value = previous;
            if (previous == 0) Flush();
        });
    }

    /// <summary>
    /// Emits a summary only when this process has encountered new dynamic objects.
    /// </summary>
    internal void Flush()
    {
        var count = Interlocked.Exchange(ref _pending, 0);
        if (count > 0) RuntimeSchemaSummary(count);
    }

    [LoggerMessage(LogLevel.Debug, "Dynamic schema {Kind} '{Name}' in database '{Database}' requires runtime DDL while Storage:SchemaMode is Verify")]
    partial void RuntimeSchemaObject(string kind, string name, string database);

    [LoggerMessage(LogLevel.Warning, "{Count} dynamic schema objects were created at runtime while Storage:SchemaMode is Verify; run chronicle --migrate-only to provision them ahead of time")]
    partial void RuntimeSchemaSummary(int count);

    sealed class Scope(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
