// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// The part of <see cref="Sink"/> that adopts the key layout an existing table already has.
/// </summary>
public partial class Sink
{
    /// <summary>
    /// Resolves the key layout of the read model table, once per sink instance. Does nothing for read models whose
    /// identifier is spelled <c language="csharp">Id</c> or <c language="csharp">id</c>, or that have none.
    /// </summary>
    /// <remarks>
    /// A table is created in the schema's own layout when it does not exist. Earlier releases ignored an identifier
    /// spelled other than <c language="csharp">Id</c> or <c language="csharp">id</c> and created tables with a synthesized
    /// <c language="csharp">Id</c> key beside a nullable column for it, and a primary key is never changed after creation,
    /// so such a table keeps being used in the layout it has. Always resolved against the primary table, so a replay table
    /// is created in the layout of the table it replaces.
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    async Task EnsureLayout()
    {
        if (!_needsLayout)
        {
            return;
        }

        Task layout;
        lock (_layoutLock)
        {
            layout = _layout ??= ResolveLayout();
        }

        try
        {
            await layout;

            // Only a layout that was adopted is published for reads; a failed one is cleared below and resolved again.
            Volatile.Write(ref _layoutResolvedForRead, true);
        }
        catch
        {
            // Let the next use try again rather than keep a failed lookup for the lifetime of the sink.
            lock (_layoutLock)
            {
                if (ReferenceEquals(_layout, layout))
                {
                    _layout = null;
                }
            }

            throw;
        }
    }

    /// <summary>
    /// Resolves the key layout for reading a table of the read model without provisioning anything.
    /// </summary>
    /// <remarks>
    /// A sink that was never initialized, after a restart or on a silo that does not own the read model, has not looked at the
    /// table, so it would read a table created in the earlier layout with the current one. The layout is read from the primary table
    /// only when that table exists; nothing is created. It is looked up once per sink, by every query that opens a table.
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    async Task EnsureLayoutForRead()
    {
        // Once resolved, a read costs only these field checks.
        if (!_needsLayout || Volatile.Read(ref _layoutResolvedForRead))
        {
            return;
        }

        Task<bool> layout;
        lock (_layoutLock)
        {
            if (_layout is not null)
            {
                // _layout is assigned before it is awaited, so a pending or failed adoption must not mark reads as resolved:
                // a failure clears _layout and the next read has to resolve the layout again.
                if (_layout.IsCompletedSuccessfully)
                {
                    Volatile.Write(ref _layoutResolvedForRead, true);
                }

                return;
            }

            layout = _readLayout ??= ResolveLayoutForRead();
        }

        var resolved = false;
        try
        {
            resolved = await layout;
            if (resolved)
            {
                Volatile.Write(ref _layoutResolvedForRead, true);
            }
        }
        finally
        {
            if (!resolved)
            {
                // Without a primary table there is no layout to adopt, and a failed lookup is not kept: try again on the next read.
                lock (_layoutLock)
                {
                    if (ReferenceEquals(_readLayout, layout))
                    {
                        _readLayout = null;
                    }
                }
            }
        }
    }

    async Task<bool> ResolveLayoutForRead()
    {
        var scope = await _database.ExistingReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        if (scope is null)
        {
            return false;
        }

        await using (scope)
        {
            var database = scope.DbContext.Database;
            AdoptLayout(await ReadTableLayout(database, database.GetDatabaseType(), _tableName));
        }

        return true;
    }

    async Task ResolveLayout()
    {
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, _tableName, _columns);
        var database = scope.DbContext.Database;
        var layout = await ReadTableLayout(database, database.GetDatabaseType(), _tableName);
        AdoptLayout(layout);
    }

    void AdoptLayout(TableLayout layout)
    {
        var identifier = _columns.First(column => column.IsKey).Name;
        if (layout.PrimaryKey.Count != 1)
        {
            return;
        }

        var primaryKey = layout.PrimaryKey[0];
        var keepsSynthesizedKey = !string.Equals(primaryKey, identifier, StringComparison.Ordinal) &&
            layout.Columns.Contains(identifier, StringComparer.Ordinal);
        if (!keepsSynthesizedKey)
        {
            return;
        }

        _columns = ProjectedColumns.ForLegacyKey(_schema, _indexes, primaryKey);
        _compositeKey = LegacyCompositeKey.For(_schema, _columns.First(column => column.IsKey));
        _logger.LegacyKeyKept(_tableName, primaryKey, identifier);
    }
}
