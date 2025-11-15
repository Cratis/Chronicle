// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

/// <summary>
/// Represents a DbContext for a specific event sequence table within a namespace.
/// </summary>
/// <remarks>
/// This DbContext is used to manage a single table per event sequence,
/// similar to how MongoDB uses a separate collection per event sequence.
/// Each instance manages a table named after the event sequence.
/// </remarks>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
/// <param name="tableName">The name of the table (event sequence name).</param>
/// <param name="migrator">The <see cref="IEventSequenceMigrator"/> for managing table migrations.</param>
public class EventSequenceDbContext(DbContextOptions<EventSequenceDbContext> options, string tableName, IEventSequenceMigrator migrator) : BaseDbContext(options), ITableDbContext
{
    /// <summary>
    /// Gets or sets the event entries DbSet.
    /// </summary>
    public DbSet<EventEntry> Events { get; set; } = null!;

    /// <summary>
    /// Ensures the table exists in the database using migrations.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EnsureTableExists()
    {
        await migrator.EnsureTableMigrated(tableName, this);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventEntry>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.SequenceNumber);
            entity.HasIndex(e => e.SequenceNumber);
            entity.HasIndex(e => e.EventSourceId);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.SequenceNumber).IsRequired();
            entity.Property(e => e.EventSourceId).IsRequired();
            entity.Property(e => e.EventSourceType).IsRequired();
            entity.Property(e => e.EventStreamId).IsRequired();
            entity.Property(e => e.EventStreamType).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Occurred).IsRequired();
        });
    }
}
