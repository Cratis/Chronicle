// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventTypes;
using Cratis.Chronicle.Storage.Sql.Namespaces;
using Cratis.Chronicle.Storage.Sql.Observers;
using Cratis.Chronicle.Storage.Sql.Projections;
using Cratis.Chronicle.Storage.Sql.Reactors;
using Cratis.Chronicle.Storage.Sql.ReadModels;
using Cratis.Chronicle.Storage.Sql.Reducers;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// DbContext for the event store.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the event types DbSet.
    /// </summary>
    public DbSet<EventType> EventTypes { get; set; }

    /// <summary>
    /// Gets or sets the namespaces DbSet.
    /// </summary>
    public DbSet<Namespace> Namespaces { get; set; }

    /// <summary>
    /// Gets or sets the observers DbSet.
    /// </summary>
    public DbSet<ObserverDefinition> Observers { get; set; }

    /// <summary>
    /// Gets or sets the projections DbSet.
    /// </summary>
    public DbSet<Projection> Projections { get; set; }

    /// <summary>
    /// Gets or sets the reactors DbSet.
    /// </summary>
    public DbSet<ReactorDefinition> Reactors { get; set; }

    /// <summary>
    /// Gets or sets the reducers DbSet.
    /// </summary>
    public DbSet<ReducerDefinition> Reducers { get; set; }

    /// <summary>
    /// Gets or sets the read models DbSet.
    /// </summary>
    public DbSet<ReadModelDefinition> ReadModels { get; set; }
}
