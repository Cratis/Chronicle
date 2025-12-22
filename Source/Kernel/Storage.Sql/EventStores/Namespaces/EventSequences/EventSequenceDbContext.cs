// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
    internal readonly string _tableName = tableName;

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
        await migrator.EnsureTableMigrated(_tableName, this);
    }

    /// <inheritdoc/>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite doesn't support unsigned 64-bit integers natively
        // Configure ulong properties to use signed INT64 in the database
        configurationBuilder
            .Properties<ulong>()
            .HaveConversion<long>();
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // CRITICAL: EF Core caches models by DbContext type + model cache key.
        // Since we use the same EventSequenceDbContext type for different tables,
        // we must include the table name in the cache key to prevent model reuse.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicTableModelCacheKeyFactory>();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventEntry>(entity =>
        {
            entity.ToTable(_tableName);
            entity.HasKey(e => e.SequenceNumber);
            entity.HasIndex(e => e.SequenceNumber);
            entity.HasIndex(e => e.EventSourceId);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.SequenceNumber).IsRequired().ValueGeneratedNever();
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
