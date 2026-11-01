// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// A metadata-only context; it never registers or creates a dynamic read-model entity.
/// </summary>
/// <param name="options">Schema-purpose options for the resolved database/schema.</param>
/// <param name="schema">The resolved schema, or null for the connection's default schema.</param>
internal sealed class ReadModelTableBindingsDbContext(DbContextOptions<ReadModelTableBindingsDbContext> options, string? schema = null) : DbContext(options)
{
    /// <summary>
    /// Gets the durable name reservations.
    /// </summary>
    internal DbSet<ReadModelTableNameBinding> Bindings => Set<ReadModelTableNameBinding>();

    /// <summary>
    /// Gets existing promotion evidence without constructing or migrating a user-table context.
    /// </summary>
    internal DbSet<ReplayPromotion> ReplayPromotions => Set<ReplayPromotion>();

    /// <summary>
    /// Gets the resolved schema included in the EF model cache identity.
    /// </summary>
    internal string? Schema => string.IsNullOrEmpty(schema) ? null : schema;

    /// <summary>
    /// Builds a ledger context on the same connection and transaction as a model context,
    /// so ledger changes commit or roll back with the table DDL they accompany.
    /// </summary>
    /// <param name="context">The model context whose transaction is current.</param>
    /// <param name="schema">The resolved schema of the ledger.</param>
    /// <returns>The enlisted ledger context, owned by the caller.</returns>
    /// <exception cref="InvalidOperationException">The model context has no current transaction.</exception>
    internal static async Task<ReadModelTableBindingsDbContext> Enlist(DbContext context, string? schema)
    {
        var connection = context.Database.GetDbConnection();
        var builder = new DbContextOptionsBuilder<ReadModelTableBindingsDbContext>();
        switch (context.Database.ProviderName)
        {
            case "Npgsql.EntityFrameworkCore.PostgreSQL":
                builder.UseNpgsql(connection);
                break;
            case "Microsoft.EntityFrameworkCore.SqlServer":
                builder.UseSqlServer(connection);
                break;
            default:
                builder.UseSqlite(connection);
                break;
        }

        var enlisted = new ReadModelTableBindingsDbContext(builder.Options, schema);
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("Enlisting a ledger context requires a current transaction.");
        await enlisted.Database.UseTransactionAsync(transaction);
        return enlisted;
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ReadModelTableBindingsModelCacheKeyFactory>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReplayPromotion>(promotion =>
        {
            promotion.ToTable(WellKnownTableNames.ReplayPromotions, Schema);
            promotion.HasKey(row => row.Id);
            promotion.Property(row => row.Id).HasMaxLength(64);
        });
        var entity = modelBuilder.Entity<ReadModelTableNameBinding>();
        entity.ToTable(WellKnownTableNames.ReadModelTableNames, Schema);
        entity.HasKey(row => row.Id);
        entity.Ignore(row => row.Owner);
        entity.Property(row => row.Id).HasMaxLength(64);
        entity.Property(row => row.PhysicalName).HasMaxLength(128);
        entity.HasIndex(row => row.PhysicalName).IsUnique();
        var binary = Database.ProviderName switch
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "C",
            "Microsoft.EntityFrameworkCore.SqlServer" => "Latin1_General_100_BIN2",
            _ => "BINARY"
        };
        entity.Property(row => row.Id).UseCollation(binary);
        if (!Database.IsSqlServer())
        {
            entity.Property(row => row.PhysicalName).UseCollation(Database.IsSqlite() ? "NOCASE" : binary);
        }

        // SQL Server deliberately inherits database collation for table-identifier uniqueness.
        // Full owner equality is ordinal CLR equality after loading the digest-keyed row.
    }
}
