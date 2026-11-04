// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents a SQL implementation of <see cref="IResourceNameReservations"/>, using the primary key of the reservations table.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/>.</param>
public class SqlResourceNameReservations(IDatabase database) : ResourceNameReservations
{
    /// <inheritdoc/>
    protected override async Task<string?> TryAdd(string key, string owner)
    {
        await using var scope = await database.Cluster();
        scope.DbContext.ResourceNameReservations.Add(new ResourceNameReservationEntity { Key = key, Owner = owner });
        try
        {
            await scope.DbContext.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            await using var reading = await database.Cluster();
            var existing = await reading.DbContext.ResourceNameReservations.AsNoTracking().SingleOrDefaultAsync(row => row.Key == key);
            return existing?.Owner ?? string.Empty;
        }
    }

    /// <inheritdoc/>
    protected override async Task Remove(IReadOnlyCollection<string> keys, string owner)
    {
        if (keys.Count == 0)
        {
            return;
        }

        await using var scope = await database.Cluster();
        await scope.DbContext.ResourceNameReservations.Where(row => keys.Contains(row.Key) && row.Owner == owner).ExecuteDeleteAsync();
    }
}
