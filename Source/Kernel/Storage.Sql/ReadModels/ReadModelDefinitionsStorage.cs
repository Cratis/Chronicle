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
public class ReadModelDefinitionsStorage(EventStoreDbContext dbContext) : IReadModelDefinitionsStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Concepts.ReadModels.ReadModelDefinition>> GetAll()
    {
        var readModels = await dbContext.ReadModels.ToListAsync();
        return readModels.Select(rm => rm.ToKernel()).ToArray();
    }

    /// <inheritdoc/>
    public Task<bool> Has(ReadModelIdentifier identifier) =>
        dbContext.ReadModels.AnyAsync(rm => rm.Id == identifier);

    /// <inheritdoc/>
    public Task<Concepts.ReadModels.ReadModelDefinition> Get(ReadModelIdentifier identifier) =>
        dbContext.ReadModels
            .Where(rm => rm.Id == identifier)
            .Select(rm => rm.ToKernel())
            .FirstOrDefaultAsync()!;

    /// <inheritdoc/>
    public async Task Save(Concepts.ReadModels.ReadModelDefinition definition)
    {
        var entity = definition.ToSql();
        dbContext.ReadModels.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(ReadModelIdentifier identifier)
    {
        var entity = await dbContext.ReadModels.FindAsync(identifier);
        if (entity != null)
        {
            dbContext.ReadModels.Remove(entity);
            await dbContext.SaveChangesAsync();
        }
    }
}
