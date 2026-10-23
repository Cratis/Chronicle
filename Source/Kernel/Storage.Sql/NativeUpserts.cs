// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Executes native upserts with the provider's transient-failure retry strategy.
/// </summary>
internal static class NativeUpserts
{
    /// <summary>
    /// Upserts an entity immediately, joining an existing transaction when present.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="set">The entity set.</param>
    /// <param name="entity">The desired values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    internal static Task Upsert<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.Interfaces)] TEntity>(
        this DbSet<TEntity> set, TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var database = set.GetService<ICurrentDbContext>().Context.Database;
        return database.CurrentTransaction is not null
            ? set.UpsertAsync(entity, cancellationToken)
            : database.CreateExecutionStrategy().ExecuteAsync(() => set.UpsertAsync(entity, cancellationToken));
    }
}
