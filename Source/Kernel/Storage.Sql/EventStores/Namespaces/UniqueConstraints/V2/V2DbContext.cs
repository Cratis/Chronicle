// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

/// <summary>
/// Owns only dormant v2 state. Internal contexts are never conventionally registered.
/// </summary>
/// <param name="options">The caller's provider and authentication configuration.</param>
/// <param name="table">The bound claim table, or null for metadata only.</param>
/// <param name="mode">The retention mode.</param>
[IgnoreConvention]
sealed class V2DbContext(DbContextOptions options, string? table = null, UniqueConstraintMode mode = UniqueConstraintMode.PerEventSource) : BaseDbContext(options)
{
    internal string? Table => table;
    internal UniqueConstraintMode Mode => mode;
    internal DbSet<V2Authority> Authorities => Set<V2Authority>();
    internal DbSet<V2Binding> Bindings => Set<V2Binding>();
    internal DbSet<V2Claim> Claims => Set<V2Claim>();

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, V2ModelCacheKeyFactory>();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureAuthority(modelBuilder);
        ConfigureBinding(modelBuilder);
        if (table is null)
        {
            modelBuilder.Ignore<V2Claim>();
        }
        else
        {
            ConfigureClaims(modelBuilder);
        }
    }

    void ConfigureAuthority(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<V2Authority>();
        entity.ToTable(V2Identity.Authorities);
        entity.HasKey(row => row.Id);
        entity.Property(row => row.Id).HasMaxLength(64);
        entity.Property(row => row.Sequence).IsRequired();
        entity.Property(row => row.Constraint).IsRequired();
        if (Database.IsSqlServer()) entity.Property(row => row.Id).UseCollation(V2Identity.BinaryCollation);
    }

    void ConfigureBinding(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<V2Binding>();
        entity.ToTable(V2Identity.Bindings);
        entity.HasKey(row => row.Id);
        entity.Property(row => row.Id).HasMaxLength(64);
        entity.Property(row => row.Table).HasMaxLength(63);
        entity.Property(row => row.Sequence).IsRequired();
        entity.Property(row => row.Constraint).IsRequired();
        entity.HasIndex(row => row.Table).IsUnique().HasDatabaseName("IX_UC2Bindings_Table");
        if (Database.IsSqlServer())
        {
            entity.Property(row => row.Id).UseCollation(V2Identity.BinaryCollation);
            entity.Property(row => row.Table).UseCollation(V2Identity.BinaryCollation);
        }
    }

    void ConfigureClaims(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<V2Claim>();
        entity.ToTable(table!);
        if (mode == UniqueConstraintMode.PerValue) entity.HasKey(row => new { row.Scope, row.Value });
        else entity.HasKey(row => new { row.Scope, row.EventSourceId });
        entity.Property(row => row.Scope).HasMaxLength(32).IsUnicode(false).IsFixedLength();
        entity.Property(row => row.Value).HasMaxLength(64).IsUnicode(false).IsFixedLength();
        entity.Property(row => row.EventSourceId).HasMaxLength(V2Identity.OwnerLimit);
        entity.Property(row => row.Subject);
        entity.Property(row => row.SequenceNumber).HasPrecision(20, 0);
        if (Database.IsSqlite()) entity.Property(row => row.SequenceNumber).HasColumnType("TEXT");
        entity.HasIndex(row => new { row.Scope, row.Value }).IsUnique().HasDatabaseName($"IX_{table}_V");
        entity.HasIndex(row => new { row.Scope, row.EventSourceId }).HasDatabaseName($"IX_{table}_O");
        if (Database.IsSqlServer())
        {
            entity.HasKey(mode == UniqueConstraintMode.PerValue ? [nameof(V2Claim.Scope), nameof(V2Claim.Value)] : [nameof(V2Claim.Scope), nameof(V2Claim.EventSourceId)]).IsClustered(false);
            entity.Property(row => row.Scope).UseCollation(V2Identity.BinaryCollation);
            entity.Property(row => row.Value).UseCollation(V2Identity.BinaryCollation);
            entity.Property(row => row.EventSourceId).UseCollation(V2Identity.BinaryCollation);
        }
        if (Database.IsNpgsql())
        {
            entity.Property(row => row.Scope).UseCollation("C");
            entity.Property(row => row.Value).UseCollation("C");
            entity.Property(row => row.EventSourceId).UseCollation("C");
        }
    }
}
