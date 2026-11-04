// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_resolving_the_key_layout.given;

/// <summary>
/// A sink for a read model whose identifier is spelled <c language="csharp">ID</c>, over a database of its own on a container provider.
/// </summary>
public abstract class a_sink_on_a_database_provider : Specification
{
    protected const string Table = "keyed_models";
    protected ISqlSinkHarness _harness;
    protected Sink _sink;
    protected int _layoutReads;
    private protected TableLayout _layout;
    protected string[] _keys;

    protected abstract ISqlSinkHarness CreateHarness();

    /// <summary>
    /// Runs the given statement against the specification's database.
    /// </summary>
    /// <param name="sql">The statement to run.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task Execute(string sql);

    /// <summary>
    /// Reads the key values of every stored row, from the column with the given name.
    /// </summary>
    /// <param name="column">The key column.</param>
    /// <returns>The stored key values.</returns>
    protected abstract Task<string[]> ReadKeys(string column);

    /// <summary>
    /// Gives the specification a chance to prepare the database after it exists and before the sink first uses the table.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task Prepare() => Task.CompletedTask;

    async Task Establish()
    {
        _harness = CreateHarness();
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"ID":{"type":"string"},"name":{"type":"string"}}}""");
        _sink = (Sink)_harness.CreateSink(new ReadModelDefinition("keyed", Table, "Keyed", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "keyed", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []));
        _sink.ReadTableLayout = async (database, type, table) =>
        {
            _layoutReads++;
            return await PrimaryKeyColumns.LayoutOf(database, type, table);
        };
        await Prepare();
    }

    protected async Task WriteAndReadLayout()
    {
        await Write("one", "first");
        await Write("two", "second");
        await Write("one", "third");

        await using var context = new DbContext(SpecDbContextOptions.ForConnection<DbContext>(_harness.ConnectionString));
        _layout = await PrimaryKeyColumns.LayoutOf(context.Database, context.Database.GetDatabaseType(), Table);
        _keys = await ReadKeys(_layout.PrimaryKey.Single());
    }

    protected async Task Replay()
    {
        var context = new ReplayContext(new ReadModelType("keyed", ReadModelGeneration.First), Table, "keyed_models_backup", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(context);
        await Write("one", "replayed first");
        await Write("two", "replayed second");
        await _sink.EndReplay(context);
    }

    void Destroy() => _harness.Dispose();

    protected Task Write(string key, string name)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, name)])]);
        return _sink.ApplyChanges(new Key(key, ArrayIndexers.NoIndexers), changeset, EventSequenceNumber.First);
    }
}
