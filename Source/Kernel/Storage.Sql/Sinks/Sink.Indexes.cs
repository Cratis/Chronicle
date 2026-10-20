// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public partial class Sink
{
    /// <inheritdoc/>
    public async Task EnsureIndexes()
    {
        if (_indexes.Count == 0)
        {
            return;
        }

        var table = ActiveTableName;
        await using var scope = await _database.ReadModelTable(_eventStoreName, _namespace, table, _columns, ConnectionPurpose.Schema);
        var context = scope.DbContext;
        var databaseType = context.Database.GetDatabaseType();
        await DatabaseSchemaLock.Run(context, async () =>
        {
            foreach (var index in _indexes)
            {
                var path = index.PropertyPath;
                var column = path.Segments.Count() == 1 ? _columns.FirstOrDefault(column => column.Name == path.Path) : null;
                if (column is not { IsJson: false, IsArray: false })
                {
                    _logger.IndexNotCreated(_tableName, path.Path, "only top-level scalar columns are supported; nested paths, objects and arrays require SQL expression indexes");
                    continue;
                }

                if (databaseType == DatabaseType.SqlServer && column.ClrType == typeof(string) && !column.IsKey)
                {
                    _logger.IndexNotCreated(_tableName, path.Path, "NVARCHAR(MAX) is not an indexable SQL Server key (nonclustered keys are limited to 1700 bytes)");
                    continue;
                }

                try
                {
                    await ReadModelIndexes.Ensure(context, table, column.Name);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // An index speeds up reads; failing to build one must not stop the read model from being served.
                    _logger.IndexCreationFailed(exception, _tableName, path.Path);
                }
            }
        });
    }
}
