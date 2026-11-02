// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction.given;

/// <summary>
/// A read model with a key, a group, a join column, a scalar and a JSON children column, behind a sink whose
/// commands, transactions and commits a specification can observe, delay or fail.
/// </summary>
/// <typeparam name="THarness">The database the sink writes to.</typeparam>
public abstract class a_cycle_with_failure_injection<THarness> : Specification
    where THarness : ISqlSinkHarness
{
    protected const string ReceiptTable = "chronicle_sink_cycle_receipts";
    protected const string TableName = "cycle_rows";
    protected THarness _harness;
    protected ISink _sink;
    protected ReadModelDefinition _definition;
    protected SqlCommandCapture _capture;
    protected Exception? _error;

    protected abstract THarness CreateHarness(SqlCommandCapture capture);

    void Establish()
    {
        _capture = new();
        _harness = CreateHarness(_capture);
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"group":{"type":"string"},"joinId":{"type":"string"},"x":{"type":"integer"},"children":{"type":"array","items":{"type":"object","properties":{"id":{"type":"string"},"label":{"type":"string"}}}}}}""");
        _definition = new ReadModelDefinition("cycle-read-model", TableName, "Cycle", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = _harness.CreateSink(_definition);
    }

    void Destroy() => _harness.Dispose();

    protected static PropertiesChanged<ExpandoObject> Set(params (string Name, object? Value)[] values) =>
        new(new ExpandoObject(), values.Select(value => new PropertyDifference(value.Name, null, value.Value)).ToList());

    protected static Joined Join(string onProperty, string key, params (string Name, object? Value)[] values) =>
        new(new ExpandoObject(), key, onProperty, ArrayIndexers.NoIndexers, [Set(values)]);

    protected static Joined KeyedFromJoin(string onProperty, string key, params (string Name, object? Value)[] values) =>
        new(new ExpandoObject(), key, onProperty, ArrayIndexers.NoIndexers, [Set(values)]) { HasKeyedFrom = true };

    protected static ChildAdded AddChild(string id) =>
        new(Child(id), "children", "id", id, ArrayIndexers.NoIndexers);

    protected static ChildRemovedFromAll RemoveChildFromAll(string id) =>
        new("children", "id", id, ArrayIndexers.NoIndexers);

    protected static ExpandoObject Child(string id)
    {
        dynamic child = new ExpandoObject();
        child.id = id;
        child.label = "label";
        return child;
    }

    protected Task Apply(string id, ulong sequenceNumber, params Change[] changes) => Apply(id, sequenceNumber, SinkWriteMode.Always, changes);

    protected async Task Apply(string id, ulong sequenceNumber, SinkWriteMode mode, params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, sequenceNumber, mode);
    }

    protected static async Task ApplyOn(ISink sink, string id, ulong sequenceNumber, params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(changes);
        await sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, sequenceNumber, SinkWriteMode.Always);
    }

    protected bool IsSqlite => _harness.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite;

    protected async Task Execute(string sql)
    {
        await using var context = OpenInspectionContext();
#pragma warning disable EF1002 // Specification-owned statements.
        await context.Database.ExecuteSqlRawAsync(sql);
#pragma warning restore EF1002
    }

    protected async Task<long> Scalar(string sql)
    {
        await using var context = OpenInspectionContext();
#pragma warning disable EF1002 // Specification-owned statements.
        return await context.Database.SqlQueryRaw<long>(sql).SingleAsync();
#pragma warning restore EF1002
    }

    protected Task<long> DatabaseNow() => Scalar($"SELECT CAST({_harness.ConnectionString.GetDatabaseType() switch { DatabaseType.PostgreSql => "(extract(epoch from clock_timestamp()) * 1000)::bigint", DatabaseType.SqlServer => "DATEDIFF_BIG(MILLISECOND, '1970-01-01', SYSUTCDATETIME())", _ => "CAST(strftime('%s', 'now') AS INTEGER) * 1000" }} AS bigint) AS \"Value\"");

    /// <summary>
    /// Runs a statement on the very connection and transaction of a command that is about to run. Needed on SQLite, where the
    /// cycle holds the write lock so that no other connection can interleave.
    /// </summary>
    /// <param name="command">The command about to run.</param>
    /// <param name="sql">The statement to run first.</param>
    /// <returns>Awaitable task.</returns>
    protected static async Task ExecuteOn(DbCommand command, string sql)
    {
#pragma warning disable CA2100 // Specification-owned statement.
        await using var other = command.Connection!.CreateCommand();
        other.Transaction = command.Transaction;
        other.CommandText = sql;
#pragma warning restore CA2100
        await other.ExecuteNonQueryAsync();
    }

    protected async Task<IDictionary<string, object?>?> Row(string id) =>
        await _sink.FindOrDefault(new Key(id, ArrayIndexers.NoIndexers));

    protected static int ChildCount(IDictionary<string, object?>? row) =>
        row is not null && row.TryGetValue("children", out var children) && children is IEnumerable<object> list ? list.Count() : 0;

    protected async Task<int> ReceiptCount()
    {
        await using var context = OpenInspectionContext();
        var database = context.Database;
        var quoted = database.GetDatabaseType() == DatabaseType.SqlServer ? $"[{ReceiptTable}]" : $"\"{ReceiptTable}\"";
        try
        {
#pragma warning disable EF1002 // Specification-owned identifier.
            return await database.SqlQueryRaw<int>($"SELECT CAST(COUNT(*) AS integer) AS \"Value\" FROM {quoted}").SingleAsync();
#pragma warning restore EF1002
        }
        catch (Exception)
        {
            // A database that never needed a receipt has no table.
            return 0;
        }
    }

    protected ISink AnotherSink() => _harness.CreateSink(_definition);

    protected string Quote(string identifier) => _harness.ConnectionString.GetDatabaseType() == DatabaseType.SqlServer ? $"[{identifier}]" : $"\"{identifier}\"";

    protected async Task<long> Watermark(string id)
    {
        await using var context = OpenInspectionContext();
#pragma warning disable EF1002 // Specification-owned identifiers and literals.
        return await context.Database.SqlQueryRaw<long>($"SELECT CAST({Quote("__lastHandledEventSequenceNumber")} AS bigint) AS \"Value\" FROM {Quote(TableName)} WHERE {Quote("id")} = '{id}'").SingleAsync();
#pragma warning restore EF1002
    }

    protected IEnumerable<string> ReceiptCommands => _capture.Commands.Where(command => command.Contains(ReceiptTable, StringComparison.Ordinal));

    protected IEnumerable<string> ReceiptProbes => ReceiptCommands.Where(command => command.Contains("EXISTS", StringComparison.OrdinalIgnoreCase));

    protected IEnumerable<string> ReceiptInserts => ReceiptCommands.Where(command => command.Contains("INSERT", StringComparison.OrdinalIgnoreCase));

    protected IEnumerable<string> ReadModelWrites => _capture.WriteCommands.Where(command => IsReadModelInsert(command) || IsReadModelUpdate(command));

    protected static bool IsReadModelInsert(string command) => SqlCommandCapture.CountStatements(command, "INSERT INTO", TableName) + SqlCommandCapture.CountStatements(command, "INSERT INTO", $"replay-{TableName}") > 0;

    protected static bool IsReadModelUpdate(string command) => SqlCommandCapture.CountStatements(command, "UPDATE", TableName) + SqlCommandCapture.CountStatements(command, "UPDATE", $"replay-{TableName}") > 0;

    protected ReadModelDbContext OpenInspectionContext()
    {
        var builder = new DbContextOptionsBuilder<ReadModelDbContext>();
        if (_harness.ConnectionString.GetDatabaseType() == DatabaseType.PostgreSql)
        {
            builder = new(SpecDbContextOptions.Kernel<ReadModelDbContext>(_harness.ConnectionString));
        }
        else
        {
            builder.UseDatabaseFromConnectionString(_harness.ConnectionString);
            builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        }

        return new ReadModelDbContext(builder.Options, TableName, ProjectedColumns.ForSchema(_definition.GetSchemaForLatestGeneration()), Substitute.For<IReadModelMigrator>());
    }
}
