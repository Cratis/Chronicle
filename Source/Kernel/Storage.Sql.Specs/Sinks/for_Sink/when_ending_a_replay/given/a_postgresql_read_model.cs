// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

/// <summary>
/// A read model accumulating a count in a PostgreSQL database of its own, where primary key names are unique
/// across the schema and identifiers longer than 63 bytes are truncated.
/// </summary>
/// <remarks>
/// The container name must be 48 bytes or less, so that its <c language="csharp">replay-</c> shadow table and
/// <c language="csharp">-yyyyMMddHHmmss</c> backups fit PostgreSQL's limit. Longer names fail through the real
/// migrator (issue #4340); their primary key naming is specified on <see cref="PrimaryKeyNames"/> alone.
/// </remarks>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/> supplying the container.</param>
public abstract class a_postgresql_read_model(PostgreSqlFixture fixture) : Specification
{
    const string SchemaJson = """
        {
          "type": "object",
          "properties": {
            "count": { "type": "integer" }
          }
        }
        """;

    protected ISink _sink;
    protected Key _key;
    protected string _keyColumn;
    protected ulong _sequenceNumber;
    PostgreSqlSinkHarness _harness;

    /// <summary>
    /// Gets the container name of the read model.
    /// </summary>
    protected abstract string ContainerName { get; }

    void Establish()
    {
        _key = new Key("counter-1", ArrayIndexers.NoIndexers);
        var definition = CreateReadModelDefinition();
        _keyColumn = ProjectedColumns.ForSchema(definition.GetSchemaForLatestGeneration()).Single(_ => _.IsKey).Name;
        _harness = new PostgreSqlSinkHarness { Fixture = fixture };
        _sink = _harness.CreateSink(definition);
    }

    void Destroy() => _harness.Dispose();

    /// <summary>
    /// Sets the count outside of any replay.
    /// </summary>
    /// <param name="count">The count to set.</param>
    /// <returns>Awaitable task.</returns>
    protected Task Write(int count) => _sink.ApplyChanges(_key, ChangesetSettingCountTo(count), ++_sequenceNumber);

    /// <summary>
    /// Replays the read model into a state holding the given count, keeping the previous state as the given backup.
    /// </summary>
    /// <param name="count">The count the replay produces.</param>
    /// <param name="revertContainerName">The container the previous state is kept in.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Replay(int count, string revertContainerName)
    {
        var context = new ReplayContext(
            new ReadModelType("test-read-model", ReadModelGeneration.First),
            ContainerName,
            revertContainerName,
            DateTimeOffset.UtcNow);

        await _sink.BeginReplay(context);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(count), ++_sequenceNumber);
        await _sink.EndReplay(context);
    }

    /// <summary>
    /// Reads the accumulated count back through the sink.
    /// </summary>
    /// <returns>The count currently stored for the read model.</returns>
    protected async Task<int> CurrentCount()
    {
        var instance = await _sink.FindOrDefault(_key);
        return Convert.ToInt32(((IDictionary<string, object?>)instance!)["count"], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the name of the primary key of a table, as the database stores it.
    /// </summary>
    /// <param name="table">The name of the table.</param>
    /// <returns>The primary key name, or null when the table has none.</returns>
    protected async Task<string?> PrimaryKeyOf(string table)
    {
        await using var connection = new NpgsqlConnection(_harness.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT k.conname FROM pg_constraint k JOIN pg_class c ON c.oid = k.conrelid WHERE k.contype = 'p' AND c.relname = $1";
        command.Parameters.AddWithValue(PrimaryKeyNames.TableIdentifier(DatabaseType.PostgreSql, table));
        return await command.ExecuteScalarAsync() as string;
    }

    /// <summary>
    /// Runs a statement against the read model's database.
    /// </summary>
    /// <param name="sql">The statement to run.</param>
    /// <returns>Awaitable task.</returns>
    protected async Task Execute(string sql)
    {
        await using var dataSource = NpgsqlDataSource.Create(_harness.ConnectionString);
        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Gets the primary key name the sink is expected to give a table.
    /// </summary>
    /// <param name="table">The name of the table.</param>
    /// <returns>The expected primary key name.</returns>
    protected static string ExpectedPrimaryKeyOf(string table) => PrimaryKeyNames.For(DatabaseType.PostgreSql, table);

    static IChangeset<AppendedEvent, ExpandoObject> ChangesetSettingCountTo(int count)
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["count"] = count;

        PropertyDifference[] differences = [new PropertyDifference(new PropertyPath("count"), null, count)];
        var propertiesChanged = new PropertiesChanged<ExpandoObject>(state, differences);

        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        Change[] changes = [propertiesChanged];
        changeset.Changes.Returns(changes);
        return changeset;
    }

    ReadModelDefinition CreateReadModelDefinition() =>
        new(
            "test-read-model",
            ContainerName,
            "Test read model",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, JsonSchema.FromJson(SchemaJson) }
            },
            []);
}
