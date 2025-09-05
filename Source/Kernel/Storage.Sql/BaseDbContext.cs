// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Json;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Base class for all DbContexts in the Chronicle system.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class BaseDbContext(DbContextOptions options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyJsonConversion(Database.ProviderName);
        base.OnModelCreating(modelBuilder);
    }
}
