// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;
using Cratis.Chronicle.Storage.Patching;
using Cratis.Chronicle.Storage.Security;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Cluster.Patching;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="ISystemStorage"/> for SQL storage.
/// </summary>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class SystemStorage(IDatabase database) : ISystemStorage
{
    const int SystemInformationId = 0;

    /// <inheritdoc/>
    public IUserStorage Users { get; } = new UserStorage(database);

    /// <inheritdoc/>
    public IApplicationStorage Applications { get; } = new ApplicationStorage(database);

    /// <inheritdoc/>
    public IDataProtectionKeyStorage DataProtectionKeys { get; } = new DataProtectionKeyStorage(database);

    /// <inheritdoc/>
    public IPatchStorage Patches { get; } = new PatchStorage(database);

    /// <inheritdoc/>
    public async Task<SystemInformation?> GetSystemInformation()
    {
        await using var scope = await database.Cluster();
        var entity = await scope.DbContext.SystemInformation.FindAsync(SystemInformationId);
        if (entity is null)
        {
            return null;
        }
        return SemanticVersion.TryParse(entity.Version, out var version) ? new SystemInformation(version!) : null;
    }

    /// <inheritdoc/>
    public async Task SetSystemInformation(SystemInformation systemInformation)
    {
        await using var scope = await database.Cluster();
        await scope.DbContext.SystemInformation.Upsert(new SystemInformationEntity
        {
            Id = SystemInformationId,
            Version = systemInformation.Version.ToString(),
        });
        await scope.DbContext.SaveChangesAsync();
    }
}
