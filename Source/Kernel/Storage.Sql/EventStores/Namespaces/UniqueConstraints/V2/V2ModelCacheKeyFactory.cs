// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Separates metadata-only models and every physical table, mode and provider.
/// </summary>
[IgnoreConvention]
sealed class V2ModelCacheKeyFactory : IModelCacheKeyFactory
{
    /// <inheritdoc/>
    public object Create(DbContext context, bool designTime)
    {
        var feature = (V2DbContext)context;
        return (context.GetType(), feature.Table, feature.Mode, context.Database.ProviderName, designTime);
    }
}
