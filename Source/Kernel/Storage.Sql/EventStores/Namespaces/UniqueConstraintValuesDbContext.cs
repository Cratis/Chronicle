// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents a table of retained unique values, keyed by value rather than event source.
/// </summary>
/// <param name="options">The database context options.</param>
/// <param name="tableName">The constraint's value table name.</param>
/// <param name="migrator">The migrator creating the table on first use.</param>
public class UniqueConstraintValuesDbContext(DbContextOptions<UniqueConstraintValuesDbContext> options, string tableName, IUniqueConstraintMigrator migrator) : BaseDbContext(options), ITableDbContext
{
    /// <inheritdoc/>
    public string TableName => tableName;

    /// <summary>
    /// Gets the retained value entries.
    /// </summary>
    public DbSet<UniqueConstraintValueEntry> Entries => Set<UniqueConstraintValueEntry>();

    /// <inheritdoc/>
    public Task EnsureTableExists() => migrator.EnsureValuesTableMigrated(tableName, this);

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, DynamicTableModelCacheKeyFactory>();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UniqueConstraintValueEntry>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey(_ => _.Value);
            entity.Property(_ => _.Value).HasMaxLength(200).IsRequired();
            entity.Property(_ => _.EventSourceId).HasMaxLength(200).IsRequired();
            entity.Property(_ => _.SequenceNumber).IsRequired();
            entity.HasIndex(_ => _.EventSourceId);
        });
    }
}
