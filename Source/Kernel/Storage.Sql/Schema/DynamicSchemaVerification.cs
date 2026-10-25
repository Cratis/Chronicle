// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.Schema;

/// <summary>
/// Collects read-only dynamic catalog mismatches throughout one verification group.
/// </summary>
internal static class DynamicSchemaVerification
{
    static readonly AsyncLocal<List<string>?> _differences = new();

    /// <summary>
    /// Gets whether DDL must be replaced with catalog inspection.
    /// </summary>
    internal static bool IsActive => _differences.Value is not null;

    /// <summary>
    /// Shares a collector with nested preparation scopes.
    /// </summary>
    /// <returns>A scope restoring the previous collector.</returns>
    internal static IDisposable Begin()
    {
        var previous = _differences.Value;
        _differences.Value ??= [];
        return new Scope(() => _differences.Value = previous);
    }

    /// <summary>
    /// Records a mismatch without stopping inspection of the group.
    /// </summary>
    /// <param name="difference">The catalog mismatch.</param>
    internal static void Report(string difference) => _differences.Value?.Add(difference);

    /// <summary>
    /// Reports all collected differences after inspecting the group.
    /// </summary>
    /// <param name="database">The credential-free database name.</param>
    /// <exception cref="SchemaNotCurrent">Thrown when any difference was recorded.</exception>
    internal static void Complete(string database)
    {
        if (_differences.Value is { Count: > 0 } differences)
        {
            throw new SchemaNotCurrent(database, "dynamic schema", differences.Distinct(StringComparer.Ordinal).ToArray());
        }
    }

    /// <summary>
    /// Inspects each table and index described by a dynamic migration.
    /// </summary>
    /// <param name="context">The connected context.</param>
    /// <param name="migration">The expected objects, never executed in this scope.</param>
    /// <returns>The inspection task.</returns>
    internal static async Task Verify(DbContext context, MigrationBuilder migration)
    {
        foreach (var table in migration.Operations.OfType<CreateTableOperation>())
        {
            foreach (var difference in await DynamicTableShape.Differences(context, table)) Report(difference);
        }
        foreach (var index in migration.Operations.OfType<CreateIndexOperation>())
        {
            await DynamicIndexShape.Verify(context, index);
        }
    }

    sealed class Scope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
