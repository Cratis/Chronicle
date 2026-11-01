// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Separates ledger models for independently resolved schemas.
/// </summary>
internal sealed class ReadModelTableBindingsModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadModelTableBindingsModelCacheKeyFactory"/> class for EF dependency injection.
    /// </summary>
    public ReadModelTableBindingsModelCacheKeyFactory()
    {
    }

    /// <inheritdoc/>
    public object Create(DbContext context, bool designTime) =>
        (context.GetType(), (context as ReadModelTableBindingsDbContext)?.Schema, designTime);
}
