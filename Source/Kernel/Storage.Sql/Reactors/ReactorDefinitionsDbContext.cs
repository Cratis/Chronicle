// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Reactors;

/// <summary>
/// Represents the database context for reactor definitions.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class ReactorDefinitionsDbContext(DbContextOptions<ReactorDefinitionsDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for reactor definitions.
    /// </summary>
    public DbSet<ReactorDefinition> Reactors { get; set; }
}
