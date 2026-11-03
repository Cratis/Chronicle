// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents a DbContext for a specific read model table within a namespace.
/// </summary>
/// <remarks>
/// Each read model has its own table whose shape is derived from the read model's JsonSchema:
/// leaf properties (primitives, dates, guids, concepts of those) become real typed columns,
/// and arrays / nested objects become a single JSON column (jsonb on PostgreSQL, nvarchar(max)
/// on SQL Server, text on SQLite). The entity is registered as a shared-type entity backed by
/// <see cref="DynamicReadModelEntity"/> with one indexer-property per <see cref="ProjectedColumn"/>,
/// so EF's change tracker gives us per-column dirty tracking — every <c language="csharp">UPDATE</c> only touches
/// the columns that actually changed, matching MongoDB's <c language="csharp">$set</c> semantics.
/// </remarks>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
/// <param name="tableName">The name of the table (read model container name).</param>
/// <param name="columns">The columns derived from the read model's schema.</param>
/// <param name="migrator">The <see cref="IReadModelMigrator"/> for managing table migrations.</param>
public class ReadModelDbContext(
    DbContextOptions<ReadModelDbContext> options,
    string tableName,
    IReadOnlyList<ProjectedColumn> columns,
    IReadModelMigrator migrator) : BaseDbContext(options), ITableDbContext
{
    static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        WriteIndented = false,
        Converters =
        {
            // Geospatial values are materialized as typed CLR values in the read model; round-trip
            // them through their GeoJSON converters so the JSON column stores GeoJSON (not the CLR
            // property layout) and reads back into the typed value.
            new PointJsonConverter(),
            new LineStringJsonConverter(),
            new PolygonJsonConverter()
        }
    };

    static readonly ValueConverter<decimal, decimal> _sqlServerDecimalConverter = new(
        value => DecimalColumnValues.ForSqlServer(value),
        value => DecimalColumnValues.FromSqlServer(value));

    static readonly ValueConverter<decimal, string> _sqliteDecimalConverter = new(
        value => value.ToString(CultureInfo.InvariantCulture),
        value => decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture));

    static readonly ValueConverter<Guid, string> _sqliteGuidConverter = new(
        v => v.ToString("D"),
        v => Guid.Parse(v));

    /// <summary>
    /// Gets the JSON serializer options used to round-trip JSON column values.
    /// </summary>
    public static JsonSerializerOptions JsonSerializerOptions { get; } = _jsonOptions;

    /// <summary>
    /// Gets the name of the read model table.
    /// </summary>
    public string TableName => tableName;

    /// <summary>
    /// Gets the column metadata used to register the dynamic entity and create the underlying table.
    /// </summary>
    public IReadOnlyList<ProjectedColumn> Columns => columns;

    /// <summary>
    /// Gets the dynamic entity set for this read model's table.
    /// </summary>
    public DbSet<DynamicReadModelEntity> Entries => Set<DynamicReadModelEntity>(tableName);

    /// <summary>
    /// Gets the completion markers sharing this context's promotion transaction.
    /// </summary>
    internal DbSet<ReplayPromotion> ReplayPromotions => Set<ReplayPromotion>();

    /// <summary>
    /// Gets a value indicating whether this context only serves a table that already exists, and so never changes its schema.
    /// </summary>
    internal bool ExistingOnly { get; private set; }

    /// <summary>
    /// Gets the receipts of committed multi-write sink cycles.
    /// </summary>
    internal DbSet<SinkCycleReceipt> SinkCycleReceipts => Set<SinkCycleReceipt>();

    /// <inheritdoc/>
    public async Task EnsureTableExists()
    {
        if (ExistingOnly)
        {
            throw new InvalidOperationException($"Table '{tableName}' is read as it is; a read never creates or alters it.");
        }

        await migrator.EnsureTableMigrated(tableName, this);
    }

    /// <summary>
    /// Creates the shared marker table before starting a promotion transaction.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task EnsureReplayPromotions() => migrator.EnsureReplayPromotions(this);

    /// <summary>
    /// Forbids this context from changing the table, so a read only ever looks at what is there.
    /// </summary>
    /// <returns>This context.</returns>
    internal ReadModelDbContext ServeExisting()
    {
        ExistingOnly = true;
        return this;
    }

    /// <summary>
    /// Creates the shared sink cycle receipt table, the first time a multi-write cycle needs it.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task EnsureSinkCycleReceipts() => migrator.EnsureSinkCycleReceipts(this);

    /// <summary>
    /// Clears stale migration state for this table and restores shadows after an old-kernel promotion.
    /// </summary>
    /// <returns>An awaitable task.</returns>
    internal Task RepairShadowColumns()
    {
        if (ExistingOnly)
        {
            return Task.CompletedTask;
        }

        migrator.ClearMigrationCache($"{Database.GetConnectionString()}:{TableName}");
        return EnsureTableExists();
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // EF caches the compiled model per (DbContext type, model cache key). We reuse the
        // ReadModelDbContext type for every read-model table so we have to include the table
        // name AND the column set in the cache key — otherwise EF would hand back the model
        // for a completely different read model whose name collided in the cache.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, ReadModelCacheKeyFactory>();
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var databaseType = Database.GetDatabaseType();

        modelBuilder.Entity<ReplayPromotion>(entity =>
        {
            entity.ToTable(WellKnownTableNames.ReplayPromotions);
            entity.HasKey(promotion => promotion.Id).HasName($"PK_{WellKnownTableNames.ReplayPromotions}");
            entity.Property(promotion => promotion.Id).HasMaxLength(64);
        })
        .Entity<SinkCycleReceipt>(entity =>
        {
            entity.ToTable(WellKnownTableNames.SinkCycleReceipts);
            entity.HasKey(receipt => receipt.Id).HasName($"PK_{WellKnownTableNames.SinkCycleReceipts}");
            entity.Property(receipt => receipt.CreatedAt).HasDefaultValueSql(SinkCycleReceipt.NowSql(databaseType)).ValueGeneratedOnAdd();
            entity.HasIndex(receipt => receipt.CreatedAt);
        })
        .SharedTypeEntity<DynamicReadModelEntity>(tableName, entity =>
        {
            entity.ToTable(tableName);

            ProjectedColumn? keyColumn = null;
            foreach (var column in columns)
            {
                var propertyBuilder = entity.IndexerProperty(column.ClrType, column.Name);
                propertyBuilder.IsRequired(!column.IsNullable);
                if (column.Name == WellKnownProperties.ReadModelInstanceInitialized)
                {
                    // False is an explicit placeholder, not EF's sentinel for using the database default.
                    propertyBuilder.HasDefaultValue(true).ValueGeneratedNever();
                }

                if (column.IsJson)
                {
                    // JSON columns store nested objects / arrays as opaque JSON text in EF and as the
                    // provider's JSON type at the DB layer. Round-tripping a string is what we want —
                    // the sink serializes to JSON before SetValue and deserializes on read.
                    propertyBuilder.HasColumnType(GetJsonColumnType(databaseType));
                }
                else if (column.ClrType == typeof(string) && databaseType == DatabaseType.SqlServer)
                {
                    propertyBuilder.HasColumnType(column.SqlServerStringType);
                }
                else if ((Nullable.GetUnderlyingType(column.ClrType) ?? column.ClrType) == typeof(decimal))
                {
                    propertyBuilder.HasColumnType(databaseType switch
                    {
                        DatabaseType.PostgreSql => "NUMERIC",
                        DatabaseType.SqlServer => "DECIMAL(38,18)",
                        _ => "TEXT"
                    });
                    if (databaseType == DatabaseType.Sqlite)
                    {
                        propertyBuilder.HasConversion(_sqliteDecimalConverter);
                    }
                    if (databaseType == DatabaseType.SqlServer)
                    {
                        propertyBuilder.HasConversion(_sqlServerDecimalConverter);
                    }
                }
                else if (column.ClrType == typeof(Guid) && databaseType == DatabaseType.Sqlite)
                {
                    // SQLite has no native Guid type; persist as canonical text via Arc's converter so
                    // a Guid round-trips identically to what the Guid column helper would produce.
                    propertyBuilder.HasConversion(_sqliteGuidConverter);
                }

                if (column.IsKey)
                {
                    keyColumn = column;
                }
            }

            if (keyColumn is not null)
            {
                entity.HasKey(keyColumn.Name).HasName(PrimaryKeyNames.For(databaseType, tableName));
            }
        });
    }

    static string GetJsonColumnType(DatabaseType databaseType) => databaseType switch
    {
        DatabaseType.PostgreSql => "jsonb",
        DatabaseType.SqlServer => "nvarchar(max)",
        _ => "TEXT"
    };
}
