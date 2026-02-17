// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Represents the database context for the cluster.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class ClusterDbContext(DbContextOptions<ClusterDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for reminders.
    /// </summary>
    public DbSet<Reminder> Reminders { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for event stores.
    /// </summary>
    public DbSet<EventStore> EventStores { get; set; }
}
