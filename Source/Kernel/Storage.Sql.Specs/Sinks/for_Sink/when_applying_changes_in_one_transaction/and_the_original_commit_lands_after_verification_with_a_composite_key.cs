// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

/// <summary>
/// The receipt key is taken because an earlier attempt of the same call committed the row. The row has no composite-key
/// stamp yet, and the cycle must still stamp it.
/// </summary>
/// <typeparam name="THarness">The database.</typeparam>
public abstract class and_the_original_commit_lands_after_verification_with_a_composite_key<THarness> : given.a_cycle_with_failure_injection<THarness>
    where THarness : ISqlSinkHarness
{
    string? _stamp;

    protected virtual bool OnTheCyclesConnection => false;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"first":{"type":"string"},"second":{"type":"string"}}},"group":{"type":"string"},"joinId":{"type":"string"},"x":{"type":"integer"}}}""");
        _definition = new ReadModelDefinition("cycle-read-model", TableName, "Cycle", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, ReadModelObserverIdentifier.Unspecified, SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, []);
        _sink = _harness.CreateSink(_definition);

        await Apply(Composite("target", "t"), 1, Set(("joinId", "J")));
        await Apply(Composite("row", "r"), 2, Set(("group", "committed")));
        await Execute($"UPDATE {Quote(TableName)} SET {Quote("__compositeKey")} = NULL");
        _capture.Clear();
        _capture.BeforeCommand(command => command.Contains(ReceiptTable, StringComparison.Ordinal) && command.Contains("INSERT", StringComparison.OrdinalIgnoreCase), async command =>
        {
            var id = command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value).OfType<Guid>().First();
            var sql = $"INSERT INTO {Quote(ReceiptTable)} ({Quote("Id")}, {Quote("TableName")}, {Quote("EventSequenceNumber")}, {Quote("CreatedAt")}) VALUES ('{id}', 'x', 1, 1)";
            if (OnTheCyclesConnection)
            {
                await ExecuteOn(command, sql);
            }
            else
            {
                await Execute(sql);
            }
        });
    }

    async Task Because()
    {
        try
        {
            await Apply(Composite("row", "r"), 3, KeyedFromJoin("joinId", "J", ("x", 7)), Set(("group", "direct")));
            await using var context = OpenInspectionContext();
#pragma warning disable EF1002 // Specification-owned identifiers.
            _stamp = await context.Database.SqlQueryRaw<string>($"SELECT {Quote("__compositeKey")} AS \"Value\" FROM {Quote(TableName)} WHERE {Quote("__compositeKey")} IS NOT NULL").SingleOrDefaultAsync();
#pragma warning restore EF1002
        }
        catch (Exception exception)
        {
            _error = exception;
        }
    }

    [Fact] public void should_not_throw() => _error.ShouldBeNull();
    [Fact] public void should_not_rerun() => _capture.Started.ShouldEqual(1);
    [Fact] public void should_stamp_the_committed_row() => _stamp.ShouldNotBeNull();

    static Key Composite(string first, string second)
    {
        IDictionary<string, object?> parts = new ExpandoObject();
        parts["first"] = first;
        parts["second"] = second;
        return new((ExpandoObject)parts, ArrayIndexers.NoIndexers);
    }

    async Task Apply(Key key, ulong sequenceNumber, params Change[] changes)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns(changes);
        await _sink.ApplyChanges(key, changeset, sequenceNumber, SinkWriteMode.Always);
    }
}
