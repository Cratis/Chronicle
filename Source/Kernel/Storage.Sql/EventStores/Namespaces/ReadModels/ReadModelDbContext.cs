// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents a DbContext for a specific read model table within a namespace.
/// </summary>
/// <remarks>
/// This DbContext manages a single table per read model, treating SQL as a document store
/// by persisting each read model instance as a JSON document alongside its identifier.
/// </remarks>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
/// <param name="tableName">The name of the table (read model container name).</param>
/// <param name="migrator">The <see cref="IReadModelMigrator"/> for managing table migrations.</param>
public class ReadModelDbContext(DbContextOptions<ReadModelDbContext> options, string tableName, IReadModelMigrator migrator) : BaseDbContext(options), ITableDbContext
{
    /// <summary>
    /// Gets the name of the read model table.
    /// </summary>
    public string TableName => tableName;

    /// <summary>
    /// Gets or sets the read model entries DbSet.
    /// </summary>
    public DbSet<ReadModelEntry> Entries { get; set; } = null!;

    /// <inheritdoc/>
    public async Task EnsureTableExists()
    {
        await migrator.EnsureTableMigrated(tableName, this);
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // CRITICAL: EF Core caches models by DbContext type + model cache key.
        // Since we use the same ReadModelDbContext type for different tables,
        // we must include the table name in the cache key to prevent model reuse.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ReadModelCacheKeyFactory>();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReadModelEntry>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Document).IsRequired();
        });
    }
}
