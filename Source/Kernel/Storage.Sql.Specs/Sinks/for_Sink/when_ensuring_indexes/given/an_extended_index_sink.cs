// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ensuring_indexes.given;

public abstract class an_extended_index_sink : Specification
{
    const string Table = "extended_indexes";
    protected readonly CapturingIndexLogger _logger = new();
    ISqlSinkHarness _harness;
    DbContext _context;
    ReadModelDefinition _definition;
    DatabaseType _type;
    ISqlGenerationHelper _helper;
    string[] _initialNames;
    string[] _reconciledNames;
    int _descending;
    bool _used;
    bool _arrayUsed;
    bool _columnRemoved;
    bool _customPreserved;
    bool _nonPersisted;
    bool _deterministic;
    bool _precise;
    int _rows;

    protected abstract ISqlSinkHarness CreateHarness();

    protected async Task VerifyIndexes()
    {
        _harness = CreateHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"number":{"type":"integer"},"details":{"type":"object","properties":{"total":{"type":"number"},"active":{"type":"boolean"}}},"lines":{"type":"array","items":{"type":"object","properties":{"productId":{"type":"string"}}}}}}""");
        _definition = new("extended", Table, "Extended", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "extended", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new("details.total"), new("lines.productId") { Properties = [new("lines.productId", true)] }]);
        var sink = (Sink)_harness.CreateSink(_definition);
        await sink.GetInstances();
        _context = new(SpecDbContextOptions.ForConnection<DbContext>(_harness.ConnectionString));
        _type = _context.Database.GetDatabaseType();
        _helper = _context.GetService<ISqlGenerationHelper>();
        var json = _type == DatabaseType.PostgreSql ? "CAST({2} AS jsonb)" : "{2}";
        var lines = _type == DatabaseType.PostgreSql ? "CAST({3} AS jsonb)" : "{3}";
        await Execute($"INSERT INTO {Quote(Table)} ({Quote("id")}, {Quote("number")}, {Quote("details")}, {Quote("lines")}, {Quote("__initialized")}) VALUES ({{0}}, {{1}}, {json}, {lines}, {{4}})", "row", 7L, "{\"total\":42.5,\"active\":true}", "[{\"productId\":\"product\"}]", true);
        await sink.EnsureIndexes();
        await sink.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        _logger.Errors.ShouldBeEmpty();
        _initialNames = await Names();
        var plan = ReadModelIndexPlan.Resolve(_definition.Indexes.First(), schema, ProjectedColumns.ForSchema(schema), _type);
        await CheckUsage(plan);
        if (_type == DatabaseType.PostgreSql)
        {
            var arrayPlan = ReadModelIndexPlan.Resolve(_definition.Indexes.Last(), schema, ProjectedColumns.ForSchema(schema), _type) ?? throw new InvalidOperationException("The array index must resolve.");
            _arrayUsed = await UsesPlan($"{Quote("lines")} @> '[{{\"productId\":\"product\"}}]'::jsonb", arrayPlan.Name(_type, Table));
        }
        await sink.EnsureIndexes();
        await sink.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        (await Names()).ShouldEqual(_initialNames);
        await Execute($"CREATE INDEX custom_number ON {Quote(Table)} ({Quote("number")})");
        if (_type == DatabaseType.SqlServer)
        {
            await Execute($"ALTER TABLE {Quote(Table)} ADD custom_computed AS ({Quote("number")} + 1)");
            _deterministic = await _context.Database.SqlQuery<int>($"SELECT COLUMNPROPERTY(OBJECT_ID({Table}), {SqlServerIndexColumns.Name(plan.Targets[0])}, 'IsDeterministic') AS [Value]").SingleAsync() == 1;
            _precise = await _context.Database.SqlQuery<int>($"SELECT COLUMNPROPERTY(OBJECT_ID({Table}), {SqlServerIndexColumns.Name(plan.Targets[0])}, 'IsPrecise') AS [Value]").SingleAsync() == 1;
            _nonPersisted = await _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.computed_columns WHERE object_id=OBJECT_ID({Table}) AND name={SqlServerIndexColumns.Name(plan.Targets[0])} AND is_persisted=0").SingleAsync() == 1;
        }
        else
        {
            _nonPersisted = true;
        }
        var changed = _definition with { Indexes = [new("number") { Properties = [new("number"), new("details.active", true)] }, new("number") { Properties = [new("number", true)] }] };
        var replacement = (Sink)_harness.CreateSink(changed);
        await replacement.ReconcileIndexes();
        await replacement.IndexBuildCompletion.WaitAsync(TimeSpan.FromSeconds(30));
        _reconciledNames = await Names();
        _descending = await DescendingKeys();
        _rows = (await replacement.GetInstances()).Instances.Count();
        _customPreserved = _reconciledNames.Contains("custom_number");
        _columnRemoved = _type != DatabaseType.SqlServer || await _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.computed_columns WHERE object_id=OBJECT_ID({Table}) AND name={SqlServerIndexColumns.Name(plan.Targets[0])}").SingleAsync() == 0;
        if (_type == DatabaseType.SqlServer)
        {
            (await _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.computed_columns WHERE object_id=OBJECT_ID({Table}) AND name='custom_computed'").SingleAsync()).ShouldEqual(1);
        }
    }

    protected void VerifyCreateNestedAndSupportedArrayIndexes() => _initialNames.Length.ShouldEqual(_type == DatabaseType.PostgreSql ? 2 : 1);
    protected void VerifyUseTheNestedIndexForALookup() => _used.ShouldBeTrue();
    protected void VerifyUseTheArrayIndexForAContainmentLookup() => _arrayUsed.ShouldBeTrue();
    protected void VerifyRemoveUndeclaredExpressionAndGinIndexes() => _reconciledNames.Intersect(_initialNames).ShouldBeEmpty();
    protected void VerifyCreateBothOrderedReplacements() => _reconciledNames.Length.ShouldEqual(3);
    protected void VerifyStoreBothDescendingKeys() => _descending.ShouldEqual(2);
    protected void VerifyRemoveAnOrphanedOwnedComputedColumn() => _columnRemoved.ShouldBeTrue();
    protected void VerifyPreserveUserIndexes() => _customPreserved.ShouldBeTrue();
    protected void VerifyComputedColumnsAreDeterministic() => _deterministic.ShouldBeTrue();
    protected void VerifyComputedColumnsArePrecise() => _precise.ShouldBeTrue();
    protected void VerifyKeepComputedColumnsNonPersisted() => _nonPersisted.ShouldBeTrue();
    protected void VerifyPreservePrimaryData() => _rows.ShouldEqual(1);
    protected void VerifyReportUnsupportedArrayPaths() => (_type == DatabaseType.PostgreSql || _logger.Warnings.Any(message => message.Contains("lines.productId", StringComparison.Ordinal))).ShouldBeTrue();
    protected void VerifyWarnAboutIgnoredArrayDirection() => _logger.Warnings.Any(message => message.Contains("direction is ignored", StringComparison.Ordinal) && !message.Contains("was not created", StringComparison.Ordinal)).ShouldBeTrue();
    protected void VerifyNotHideIndexCreationFailures() => _logger.Errors.ShouldBeEmpty();

    string Quote(string name) => _helper.DelimitIdentifier(name);

    async Task<string[]> Names()
    {
        var names = await (_type switch
        {
            DatabaseType.PostgreSql => _context.Database.SqlQuery<string>($"SELECT i.relname AS \"Value\" FROM pg_index x JOIN pg_class t ON t.oid=x.indrelid JOIN pg_class i ON i.oid=x.indexrelid JOIN pg_namespace n ON n.oid=t.relnamespace WHERE t.relname={Table} AND n.nspname=current_schema() AND NOT x.indisprimary").ToArrayAsync(),
            DatabaseType.SqlServer => _context.Database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.indexes WHERE object_id=OBJECT_ID({Table}) AND is_primary_key=0 AND name IS NOT NULL").ToArrayAsync(),
            _ => _context.Database.SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type='index' AND tbl_name={Table} AND sql IS NOT NULL").ToArrayAsync()
        });
        return names.Where(name => !name.StartsWith("chronicle_imx_", StringComparison.Ordinal)).ToArray();
    }

    Task<int> DescendingKeys() => _type switch
    {
        DatabaseType.PostgreSql => _context.Database.SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_indexes WHERE schemaname=current_schema() AND tablename={Table} AND indexdef LIKE '% DESC%'").SingleAsync(),
        DatabaseType.SqlServer => _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.index_columns c JOIN sys.indexes i ON i.object_id=c.object_id AND i.index_id=c.index_id WHERE i.object_id=OBJECT_ID({Table}) AND c.is_descending_key=1").SingleAsync(),
        _ => _context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='index' AND tbl_name={Table} AND sql LIKE '% DESC%'").SingleAsync()
    };

    async Task CheckUsage(ReadModelIndexPlan plan)
    {
        var expression = _type == DatabaseType.SqlServer ? ReadModelIndexPlan.SqlServerExpression(plan.Targets[0], _helper) : ReadModelIndexPlan.Expression(plan.Targets[0], _type, _helper);
        var name = plan.Name(_type, Table);
        if (_type == DatabaseType.SqlServer)
        {
            await CheckSqlServerUsage(expression, name);
            return;
        }
        await _context.Database.OpenConnectionAsync();
        if (_type == DatabaseType.PostgreSql) await Execute("SET enable_seqscan=off");
        _used = await UsesPlan($"{expression}=42.5", name);
    }

    async Task CheckSqlServerUsage(string expression, string name)
    {
        await _context.Database.OpenConnectionAsync();
        await using var command = _context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Provider-delimited test identifiers and schema-derived expression.
        command.CommandText = $"INSERT INTO {Quote(Table)} ([id], [number], [details], [lines], [__initialized]) SELECT CONCAT('seed-', value), value, CONCAT('{{\"total\":', value, '}}'), '[]', 1 FROM GENERATE_SERIES(1000, 5095)";
        await command.ExecuteNonQueryAsync();
        command.CommandText = $"SELECT [number] FROM {Quote(Table)} WHERE {expression}=N'42.5'";
#pragma warning restore CA2100
        await Execute("SET SHOWPLAN_XML ON");
        try
        {
            var plan = XDocument.Parse((string)(await command.ExecuteScalarAsync())!);
            _used = plan.Descendants().Any(element => element.Name.LocalName == "RelOp" && (string?)element.Attribute("LogicalOp") == "Index Seek" && element.Descendants().Any(child => child.Name.LocalName == "Object" && (string?)child.Attribute("Index") == $"[{name}]"));
        }
        finally
        {
            await Execute("SET SHOWPLAN_XML OFF");
        }
        var matched = (int)(await command.ExecuteScalarAsync())!;
        matched.ShouldEqual(7);
        await Execute($"DELETE FROM {Quote(Table)} WHERE [id] LIKE 'seed-%'");
    }

    async Task<bool> UsesPlan(string predicate, string name)
    {
        var prefix = _type == DatabaseType.PostgreSql ? "EXPLAIN" : "EXPLAIN QUERY PLAN";
        await using var command = _context.Database.GetDbConnection().CreateCommand();
#pragma warning disable CA2100 // Only provider-delimited task-owned identifiers and schema-derived expressions.
        command.CommandText = $"{prefix} SELECT {Quote("number")} FROM {Quote(Table)} WHERE {predicate}";
#pragma warning restore CA2100
        await using var reader = await command.ExecuteReaderAsync();
        var lines = new List<string>();
        while (await reader.ReadAsync()) lines.Add(reader.GetString(_type == DatabaseType.PostgreSql ? 0 : 3));
        return lines.Exists(line => line.Contains(name, StringComparison.Ordinal));
    }

    Task<int> Execute(string sql, params object[] parameters)
    {
#pragma warning disable EF1002 // Test identifiers are provider-delimited, values are bound parameters.
        return _context.Database.ExecuteSqlRawAsync(sql, parameters);
#pragma warning restore EF1002
    }

    async Task Destroy()
    {
        if (_context is not null) await _context.DisposeAsync();
        _harness?.Dispose();
    }
}
