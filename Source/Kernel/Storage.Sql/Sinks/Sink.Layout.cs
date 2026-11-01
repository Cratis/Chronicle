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
