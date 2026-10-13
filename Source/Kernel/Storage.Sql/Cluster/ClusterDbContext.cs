// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Cluster.Patching;
using Cratis.Chronicle.Storage.Sql.Cluster.Security;
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

    /// <summary>
    /// Gets or sets the DbSet for users.
    /// </summary>
    public DbSet<UserEntity> Users { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for OAuth applications.
    /// </summary>
    public DbSet<ApplicationEntity> Applications { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for data protection keys.
    /// </summary>
    public DbSet<DataProtectionKeyEntity> DataProtectionKeys { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for applied patches.
    /// </summary>
    public DbSet<PatchEntity> Patches { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for system information.
    /// </summary>
    public DbSet<SystemInformationEntity> SystemInformation { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for OAuth tokens.
    /// </summary>
    public DbSet<TokenEntity> Tokens { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for OAuth authorizations.
    /// </summary>
    public DbSet<AuthorizationEntity> Authorizations { get; set; }

    /// <summary>
    /// Gets or sets the DbSet for OAuth scopes.
    /// </summary>
    public DbSet<ScopeEntity> Scopes { get; set; }

    /// <summary>
    /// Gets the permanent logical database bindings.
    /// </summary>
    internal DbSet<DatabaseNameBinding> DatabaseNames => Set<DatabaseNameBinding>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DatabaseNameBinding>(entity =>
        {
            entity.ToTable("DatabaseNames");
            entity.HasKey(row => new { row.Kind, row.EventStore, row.Namespace });
            entity.Property(row => row.EventStore).HasMaxLength(200);
            entity.Property(row => row.Namespace).HasMaxLength(200);
            entity.Property(row => row.PhysicalName).HasMaxLength(128);
            entity.HasIndex(row => row.PhysicalName).IsUnique();
            var collation = Database.ProviderName switch
            {
                "Microsoft.EntityFrameworkCore.SqlServer" => "Latin1_General_100_BIN2",
                "Npgsql.EntityFrameworkCore.PostgreSQL" => "C",
                _ => "BINARY"
            };
            entity.Property(row => row.EventStore).UseCollation(collation);
            entity.Property(row => row.Namespace).UseCollation(collation);
            if (Database.IsNpgsql())
            {
                entity.Property(row => row.PhysicalName).UseCollation("C");
            }
        });
    }
}
