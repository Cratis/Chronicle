// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Best-effort, post-commit stamping of composite keys without changing legacy primary keys or EF mappings.
/// </summary>
internal static partial class SqlCompositeKeyShadow
{
    /// <summary>
    /// The sink-owned, unmapped composite-key stamp column.
    /// </summary>
    internal const string ColumnName = "__compositeKey";
    const int MaximumLength = 850;
    static readonly Meter _meter = new("Cratis.Chronicle.Storage.Sql.CompositeKeyShadow");
    static readonly Counter<long> _failures = _meter.CreateCounter<long>("chronicle.sql.composite_key.stamp_failures");
    static readonly Counter<long> _collisions = _meter.CreateCounter<long>("chronicle.sql.composite_key.collisions");

    /// <summary>
    /// Stamps a committed row if no kernel has stamped it yet. Never fails the original write.
    /// </summary>
    /// <param name="context">The context for the active table, outside the write transaction.</param>
    /// <param name="key">The complete composite key of the written row.</param>
    /// <param name="id">The unchanged legacy primary key.</param>
    /// <param name="logger">The sink logger.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Stamp(ReadModelDbContext context, Key key, string id, ILogger<Sink> logger)
    {
        if (key.Value is not ExpandoObject parts || !context.Columns.Any(column => column.IsCompositeKey))
        {
            return;
        }

        try
        {
            var encoding = CompositeKeyEncoding.Encode(parts);
            if (encoding.Length > MaximumLength)
            {
                EncodingTooLong(logger, context.TableName, id, MaximumLength);
                return;
            }

            var stored = await ShadowColumns.Access(context, ColumnName, () => StampIfNull(context, id, encoding));
            if (CompositeKeyEncoding.IsEncoded(stored) && !string.Equals(stored, encoding, StringComparison.Ordinal))
            {
                _collisions.Add(1);
                KeyCollision(logger, context.TableName, id);
            }
        }
        catch (Exception exception)
        {
            _failures.Add(1);
            StampingFailed(logger, exception, context.TableName, id);
        }
    }

    static async Task<string?> StampIfNull(ReadModelDbContext context, string id, string encoding)
    {
        var helper = context.GetService<ISqlGenerationHelper>();
        var table = helper.DelimitIdentifier(context.TableName);
        var column = helper.DelimitIdentifier(ColumnName);
        var key = helper.DelimitIdentifier(context.Columns.Single(column => column.IsKey).Name);
#pragma warning disable EF1002 // Only delimited identifiers are interpolated. Values use SQL parameters.
        await context.Database.ExecuteSqlRawAsync($"UPDATE {table} SET {column} = {{0}} WHERE {key} = {{1}} AND {column} IS NULL", encoding, id);
        return await context.Database.SqlQueryRaw<string>($"SELECT {column} AS {helper.DelimitIdentifier("Value")} FROM {table} WHERE {key} = {{0}}", id).SingleOrDefaultAsync();
#pragma warning restore EF1002
    }

    [LoggerMessage(LogLevel.Warning, "SQL composite-key encoding for {Table} at legacy id {Id} exceeds {MaximumLength} characters; the row was not stamped")]
    static partial void EncodingTooLong(ILogger<Sink> logger, string table, string id, int maximumLength);

    [LoggerMessage(LogLevel.Warning, "SQL composite-key collision for {Table} at legacy id {Id}; preserved the existing v1 shadow stamp and the legacy write behavior")]
    static partial void KeyCollision(ILogger<Sink> logger, string table, string id);

    [LoggerMessage(LogLevel.Warning, "SQL composite-key stamping failed for {Table} at legacy id {Id}; the committed write is unchanged")]
    static partial void StampingFailed(ILogger<Sink> logger, Exception exception, string table, string id);
}
