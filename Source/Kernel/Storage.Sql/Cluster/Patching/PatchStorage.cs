// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patching;
using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.Patching;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchStorage"/> for SQL.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class PatchStorage(IDatabase database) : IPatchStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Patch>> GetAll()
    {
        await using var scope = await database.Cluster();
        var entities = await scope.DbContext.Patches.ToListAsync();
        return entities.Select(e => new Patch(e.Name, SemanticVersion.Parse(e.Version), e.AppliedAt)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(Patch patch)
    {
        await using var scope = await database.Cluster();
        var entity = new PatchEntity
        {
            Name = patch.Name,
            Version = patch.Version.ToString(),
            AppliedAt = patch.AppliedAt,
        };
        await scope.DbContext.Patches.Upsert(entity);
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> Has(string patchName)
    {
        await using var scope = await database.Cluster();
        return await scope.DbContext.Patches.AnyAsync(p => p.Name == patchName);
    }

    /// <inheritdoc/>
    public async Task Remove(string patchName)
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.Patches.FindAsync(patchName);
        if (entity is not null)
        {
            scope.DbContext.Patches.Remove(entity);
            await scope.DbContext.SaveChangesAsync();
        }
    }
}
