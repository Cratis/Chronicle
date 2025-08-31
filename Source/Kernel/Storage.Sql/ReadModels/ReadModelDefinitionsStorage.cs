// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelDefinitionsStorage"/> for SQL.
/// </summary>
/// <param name="dbContext">The database context.</param>
public class ReadModelDefinitionsStorage(ReadModelsDbContext dbContext) : IReadModelDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<ReadModelDefinition>> GetAll()
    {
        var readModels = await dbContext.ReadModels.ToListAsync();
        return readModels.Select(rm => rm.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReadModelName name) =>
        dbContext.ReadModels.AnyAsync(rm => rm.Id == name);

    /// <inheritdoc/>
    public Task<ReadModelDefinition> Get(ReadModelName name) =>
        dbContext.ReadModels
            .Where(rm => rm.Id == name)
            .Select(rm => rm.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Save(ReadModelDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.ReadModels.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(ReadModelName name)
    {
        var entity = await dbContext.ReadModels.FindAsync(name);
        if (entity != null)
        {
            dbContext.ReadModels.Remove(entity);
            await dbContext.SaveChangesAsync();
        }
    }
}
