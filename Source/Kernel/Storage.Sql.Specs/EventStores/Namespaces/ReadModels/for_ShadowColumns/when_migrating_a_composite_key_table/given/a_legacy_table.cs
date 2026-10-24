// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumns.when_migrating_a_composite_key_table.given;

public abstract class a_legacy_table : Specification
{
    protected int _scalarShadows;
    protected int _nullableShadows;
    protected int _indexes;
    protected int _unstampedRows;
    protected string _columnType;
    protected string _collisionValue;
    protected bool _collisionWarned;
    protected bool _refused;
    protected bool _mapped;
    protected int _duplicates;
    a_competing_kernel _competing;
    ISqlSinkHarness _harness;
    DbContextOptions<ReadModelDbContext> _options;
    IReadOnlyList<ProjectedColumn> _columns;
    readonly List<string> _warnings = [];
    readonly ILogger<ReadModelMigrator> _logger = Substitute.For<ILogger<ReadModelMigrator>>();

    protected abstract ISqlSinkHarness CreateHarness();

    void Establish()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!)).Do(call => _warnings.Add(call[2]!.ToString()!));
        _harness = CreateHarness();
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}}}}""");
        _columns = ProjectedColumns.ForSchema(schema);
        _harness.CreateSink(new ReadModelDefinition("migration", "composite_migration", "Migration", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "migration", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        builder.UseDatabaseFromConnectionString(_harness.ConnectionString);
        _competing = new a_competing_kernel(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()));
        _options = builder.Options;
    }

    protected async Task MigrateAndCheck()
    {
        var scalarColumns = _columns.Select(column => column with { IsCompositeKey = false }).ToArray();
        await using (var old = Context("composite_migration", scalarColumns))
        {
            await old.EnsureTableExists();
            _scalarShadows = await NullableShadows(old);
            var row = new DynamicReadModelEntity { ["id"] = "legacy_with_ambiguous_parts", ["__initialized"] = true };
            old.Entries.Add(row);
            await old.SaveChangesAsync();
        }

        // Independent migrators also adopt the column added by the competing older kernel.
        await using var first = Context("composite_migration", _columns);
        await using var second = Context("composite_migration", _columns);
        await Task.WhenAll(first.EnsureTableExists(), second.EnsureTableExists());
        await first.EnsureTableExists();
        _nullableShadows = await NullableShadows(first);
        _duplicates = _competing.Duplicates;
        _columnType = ShadowColumns.For(first).Single().ColumnType;
        _mapped = first.Model.FindEntityType(first.TableName)!.FindProperty("__compositeKey") is not null;
        _unstampedRows = await first.Database.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM composite_migration WHERE \"__compositeKey\" IS NULL").SingleAsync();
        _indexes = await Indexes(first);
        await CheckCollision();
    }

    async Task CheckCollision()
    {
        var columns = _columns.Append(new ProjectedColumn("__compositeKey", typeof(string), false, false, false, true)).ToArray();
        await using var collision = Context("composite_collision", columns);
        await collision.EnsureTableExists();
        var row = new DynamicReadModelEntity { ["id"] = "a_b", ["__compositeKey"] = "user property", ["__initialized"] = true };
        collision.Entries.Add(row);
        await collision.SaveChangesAsync();
        _collisionValue = (string)(await collision.Entries.SingleAsync())["__compositeKey"]!;
        _collisionWarned = _warnings.Exists(message => message.Contains("__compositeKey", StringComparison.Ordinal) && message.Contains("composite_collision", StringComparison.Ordinal));
        try
        {
            await ShadowColumns.Access(collision, "__compositeKey", () => Task.FromResult(0));
        }
        catch (InvalidOperationException)
        {
            _refused = true;
        }
    }

    ReadModelDbContext Context(string table, IReadOnlyList<ProjectedColumn> columns) => new(_options, table, columns, new ReadModelMigrator(_competing, _logger));

    static Task<int> NullableShadows(ReadModelDbContext context) => context.Database.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM information_schema.columns WHERE table_schema=current_schema() AND table_name={context.TableName} AND column_name='__compositeKey' AND is_nullable='YES' AND column_default IS NULL").SingleAsync(),
        DatabaseType.SqlServer => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.columns WHERE object_id=OBJECT_ID({context.TableName}) AND name='__compositeKey' AND is_nullable=1 AND default_object_id=0").SingleAsync(),
        _ => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_table_xinfo({context.TableName}) WHERE name='__compositeKey' AND \"notnull\"=0 AND dflt_value IS NULL AND hidden=0").SingleAsync()
    };

    static Task<int> Indexes(ReadModelDbContext context) => context.Database.GetDatabaseType() switch
    {
        DatabaseType.PostgreSql => context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={context.TableName} AND indexname LIKE 'chronicle_shx_%'").SingleAsync(),
        DatabaseType.SqlServer => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({context.TableName}) AND name LIKE 'chronicle_shx_%'").SingleAsync(),
        _ => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list({context.TableName}) WHERE name LIKE 'chronicle_shx_%'").SingleAsync()
    };

    void Destroy() => _harness.Dispose();
}
