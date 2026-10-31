// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Storage.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents SQL storage of administrator privileges.
/// </summary>
/// <param name="database">The system database.</param>
public class AdministratorStorage(IDatabase database) : IAdministratorStorage
{
    /// <inheritdoc/>
    public async Task<IEnumerable<Administrator>> GetAll()
    {
        await using var scope = await database.Cluster();
        var rows = await scope.DbContext.Administrators.AsNoTracking().ToListAsync();
        return rows.Select(row => new Administrator(row.Id, (GrantReason)row.Reason, row.Evidence)).ToArray();
    }

    /// <inheritdoc/>
    public async Task Save(Administrator administrator)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Administrators.Upsert(new AdministratorEntity
        {
            Id = administrator.Id.Value,
            Reason = (int)administrator.Reason,
            Evidence = administrator.Evidence
        });
        await scope.DbContext.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task Delete(UserId userId)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.Administrators.Where(row => row.Id == userId.Value).ExecuteDeleteAsync();
    }
}
