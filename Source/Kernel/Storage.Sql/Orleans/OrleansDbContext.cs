// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Orleans;

/// <summary>
/// Represents the database context for Orleans.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class OrleansDbContext(DbContextOptions<OrleansDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets DbSet for reminders.
    /// </summary>
    public DbSet<Reminder> Reminders { get; set; }
}
