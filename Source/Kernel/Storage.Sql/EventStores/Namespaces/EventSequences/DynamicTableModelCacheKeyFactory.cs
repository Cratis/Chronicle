// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents a model cache key factory that includes the table name in the cache key.
/// This ensures that each EventSequenceDbContext instance with a different table name
/// gets its own cached model, preventing table name conflicts.
/// </summary>
public class DynamicTableModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc/>
    public object Create(DbContext context, bool designTime)
    {
        if (context is EventSequenceDbContext eventSequenceContext)
        {
            // Include the table name in the cache key to ensure different tables get different models
            return (context.GetType(), eventSequenceContext._tableName, designTime);
        }

        return (context.GetType(), designTime);
    }
}
