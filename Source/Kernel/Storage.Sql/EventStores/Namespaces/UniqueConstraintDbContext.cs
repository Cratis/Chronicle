// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents a DbContext for a specific unique constraint table within a namespace.
/// </summary>
/// <remarks>
/// This DbContext is used to manage a single table per constraint name,
/// similar to how MongoDB uses a separate collection per constraint.
/// Each instance manages a table named after the constraint.
/// </remarks>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
/// <param name="tableName">The name of the table (constraint name).</param>
/// <param name="migrator">The <see cref="IUniqueConstraintMigrator"/> for managing table migrations.</param>
public class UniqueConstraintDbContext(DbContextOptions<UniqueConstraintDbContext> options, string tableName, IUniqueConstraintMigrator migrator) : BaseDbContext(options), ITableDbContext
{
    /// <summary>
    /// Gets or sets the unique constraint index entries DbSet.
    /// </summary>
    public DbSet<UniqueConstraintIndexEntry> Entries { get; set; } = null!;

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

        modelBuilder.Entity<UniqueConstraintIndexEntry>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.EventSourceId);
            entity.HasIndex(e => e.Value);
            entity.Property(e => e.EventSourceId).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.SequenceNumber).IsRequired();
        });
    }
}
