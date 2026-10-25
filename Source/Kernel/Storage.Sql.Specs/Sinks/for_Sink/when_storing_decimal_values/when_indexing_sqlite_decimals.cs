// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations.Schema;
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
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

#pragma warning disable EF1002 // Constant identifiers and internal index names only; values are parameterized.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_storing_decimal_values;

public class when_indexing_sqlite_decimals : Specification
{
    SqlSinkHarness _harness;
    Sink _sink;
    ReadModelDefinition _definition;
    readonly SqlReadCapture _capture = new();
    int _joinedRows;
    string _candidateSql;
    string[] _matches;
    string[] _range;
    string _plan;
    bool _preservedUnique;
    bool _preservedCustom;
    bool _removedLegacy;
    string[] _shadowIndexes;
    string[] _promotedIndexes;
    string[] _remainingIndexes;
    string _firstShadowIndex;
    string[] _names;

    void Establish()
    {
        _harness = new SqlSinkHarness { Interceptors = [_capture] };
        var schema = JsonSchema.FromJson("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"},"name":{"type":"string"}}}""");
        _definition = new ReadModelDefinition("decimal-indexes", "decimal_indexes", "Decimal indexes", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Projection, "decimal-indexes", SinkDefinition.None, new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = schema }, [new IndexDefinition("amount")]);
        _sink = (Sink)_harness.CreateSink(_definition);
    }

    async Task Because()
    {
        await Write("first", 193.58m);
        await Write("second", 193.580m);
        await Write("negative", -10m);
        await Write("other", 2m);
        await Write("null", null);
        await using var context = new DbContext(new DbContextOptionsBuilder().UseSqlite(_harness.ConnectionString).Options);
        var legacy = ReadModelIndexNames.For(DatabaseType.Sqlite, "decimal_indexes", "amount");
        await context.Database.ExecuteSqlRawAsync($"CREATE INDEX \"{legacy}\" ON decimal_indexes(amount)");
        await context.Database.ExecuteSqlRawAsync("CREATE INDEX manual_amount ON decimal_indexes(amount); CREATE UNIQUE INDEX chronicle_idx_unique_name ON decimal_indexes(id)");
        await _sink.EnsureIndexes();
        await _sink.EnsureIndexes();
        var indexes = await Indexes(context, "decimal_indexes");
        _firstShadowIndex = indexes.Single(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal));
        _removedLegacy = !indexes.Contains(legacy);
        _preservedUnique = indexes.Contains("chronicle_idx_unique_name");
        _preservedCustom = indexes.Contains("manual_amount");

        // Simulate an old replay promotion while the migrator and index-build caches are warm. Chronicle owns and
        // renames every chronicle_idx_ index on promotion, so the hand-made unique index is not recreated here.
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE old_decimal_indexes AS SELECT * FROM decimal_indexes; DROP TABLE decimal_indexes; CREATE TABLE decimal_indexes(id TEXT NOT NULL PRIMARY KEY, amount TEXT NULL, name TEXT NULL, __lastHandledEventSequenceNumber INTEGER NULL, __initialized INTEGER NOT NULL DEFAULT 1); INSERT INTO decimal_indexes SELECT id,amount,name,__lastHandledEventSequenceNumber,__initialized FROM old_decimal_indexes; DROP TABLE old_decimal_indexes; CREATE INDEX manual_amount ON decimal_indexes(amount)");
        _capture.Clear();
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.Changes.Returns([new Joined(new ExpandoObject(), new Amount(193.580m), "amount", ArrayIndexers.NoIndexers, [new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("name", null, "joined")])])]);
        (await _sink.ApplyChanges(new Key("first", ArrayIndexers.NoIndexers), changeset, 2UL)).ShouldBeEmpty();
        _joinedRows = _capture.Rows;
        _candidateSql = _capture.Commands.Last(command => command.Contains("WHERE", StringComparison.Ordinal) && command.Contains("__order_amount", StringComparison.Ordinal));
        _matches = await context.Database.SqlQuery<string>($"SELECT id AS Value FROM decimal_indexes WHERE __order_amount = {SqliteDecimalSortKey.For(193.58m)} ORDER BY id").ToArrayAsync();
        _range = await context.Database.SqlQuery<string>($"SELECT id AS Value FROM decimal_indexes WHERE __order_amount >= {SqliteDecimalSortKey.For(0m)} AND __order_amount < {SqliteDecimalSortKey.For(10m)} ORDER BY __order_amount").ToArrayAsync();
        _firstShadowIndex = (await Indexes(context, "decimal_indexes")).Single(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal));
        var plan = await context.Database.SqlQuery<QueryPlan>($"EXPLAIN QUERY PLAN SELECT id FROM decimal_indexes WHERE __order_amount = {SqliteDecimalSortKey.For(193.58m)}").ToArrayAsync();
        _plan = string.Join(' ', plan.Select(row => row.Detail));
        _names = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM decimal_indexes WHERE id IN ('first','second')").ToArrayAsync();
        var replay = new ReplayContext(new ReadModelType("decimal-indexes", ReadModelGeneration.First), "decimal_indexes", "decimal_indexes_revert", DateTimeOffset.UtcNow);
        await _sink.BeginReplay(replay);
        _shadowIndexes = await Indexes(context, "replay-decimal_indexes");
        await Write("replayed", 9m);
        await _sink.EndReplay(replay);
        await _sink.EnsureIndexes();
        _promotedIndexes = await Indexes(context, "decimal_indexes");
        var changed = (Sink)_harness.CreateSink(_definition with { Indexes = [] });
        await changed.ReconcileIndexes();
        _remainingIndexes = await Indexes(context, "decimal_indexes");
    }

    async Task Write(string id, decimal? amount)
    {
        var changeset = Substitute.For<IChangeset<AppendedEvent, ExpandoObject>>();
        changeset.InitialState.Returns(new ExpandoObject());
        changeset.Changes.Returns([new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference("amount", null, amount), new PropertyDifference("name", null, id)])]);
        (await _sink.ApplyChanges(new Key(id, ArrayIndexers.NoIndexers), changeset, 1UL)).ShouldBeEmpty();
    }

    static async Task<string[]> Indexes(DbContext context, string table) => await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({table})").ToArrayAsync();

    void Destroy() => _harness.Dispose();

    [Fact] void should_replace_the_old_text_index() => _removedLegacy.ShouldBeTrue();
    [Fact] void should_preserve_custom_indexes() => _preservedCustom.ShouldBeTrue();
    [Fact] void should_preserve_chronicle_prefixed_unique_indexes() => _preservedUnique.ShouldBeTrue();
    [Fact] void should_push_the_canonical_equality_into_sql() => _candidateSql.ShouldContain("__order_amount");
    [Fact] void should_materialize_only_the_equal_scaled_candidates() => _joinedRows.ShouldEqual(2);
    [Fact] void should_join_every_equal_scaled_parent() => _names.ShouldEqual(["joined", "joined"]);
    [Fact] void should_match_numerically_equal_scales_in_sql() => _matches.ShouldEqual(["first", "second"]);
    [Fact] void should_compare_numeric_ranges_in_sql() => _range.ShouldEqual(["other"]);
    [Fact] void should_use_the_generated_column_index() => _plan.ShouldContain(_firstShadowIndex);
    [Fact] void should_index_the_replay_table_shadow() => _shadowIndexes.Count(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_retain_the_promoted_shadow_index() => _promotedIndexes.Count(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_not_resurrect_the_text_index_after_promotion() => _promotedIndexes.ShouldNotContain(ReadModelIndexNames.For(DatabaseType.Sqlite, "decimal_indexes", "amount"));
    [Fact] void should_remove_the_shadow_index_when_the_declaration_is_removed() => _remainingIndexes.Any(name => name.StartsWith(ShadowColumnIndexes.Prefix, StringComparison.Ordinal)).ShouldBeFalse();

    record Amount(decimal Value) : ConceptAs<decimal>(Value);

    sealed class QueryPlan
    {
        [Column("id")]
        public long Id { get; set; }
        [Column("parent")]
        public long Parent { get; set; }
        [Column("notused")]
        public long NotUsed { get; set; }
        [Column("detail")]
        public string Detail { get; set; } = string.Empty;
    }
}
