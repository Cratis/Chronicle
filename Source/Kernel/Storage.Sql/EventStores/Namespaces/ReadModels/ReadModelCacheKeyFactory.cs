// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents a model cache key factory that includes the table name AND the column schema
/// in the cache key. Two different read models can declare the same table name (for example,
/// integration specs that re-use a generic CLR type name across scenarios), so the column
/// set must participate in the cache key — otherwise EF Core hands back a cached model whose
/// PK ClrType belongs to a different read model and any subsequent <c>Add</c> on the wrong
/// shape throws <see cref="InvalidCastException"/> inside the change tracker.
/// </summary>
public class ReadModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc/>
    public object Create(DbContext context, bool designTime)
    {
        if (context is ReadModelDbContext readModelContext)
        {
            var columnsKey = string.Join(
                '|',
                readModelContext.Columns.Select(c => $"{c.Name}:{c.ClrType.FullName}:{c.IsKey}:{c.IsJson}:{c.IsArray}:{c.IsNullable}"));
            return (context.GetType(), readModelContext.TableName, columnsKey, designTime);
        }

        return (context.GetType(), designTime);
    }
}
