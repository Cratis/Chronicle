// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Projections;

/// <summary>
/// Represents the database context for projections.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class ProjectionsDbContext(DbContextOptions<ProjectionsDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for projections.
    /// </summary>
    public DbSet<Projection> Projections { get; set; }
}
