// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventTypes;

/// <summary>
/// Represents the database context for the cluster.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class EventTypesDbContext(DbContextOptions<EventTypesDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the DbSet for event types.
    /// </summary>
    public DbSet<EventType> EventTypes { get; set; }
}
