// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_the_table_was_legacy_before_the_replay : a_composite_keyed_sql_read_model
{
    const string Table = "composite_keys";

    DynamicReadModelEntity[] _rows;
    IReadOnlyDictionary<string, string> _formats;
    bool _migrationRequired;

    async Task Because()
    {
        var legacy = await ReopenAsLegacy();
        var replay = new ReplayContext(new ReadModelType("composite", ReadModelGeneration.First), Table, $"{Table}-revert", DateTimeOffset.UtcNow);
        await legacy.BeginReplay(replay);
        await legacy.ApplyChanges(CompositeKey("a_b", "c"), ChangesetNamed("one"), 1UL);
        await legacy.ApplyChanges(CompositeKey("a", "b_c"), ChangesetNamed("two"), 2UL);
        await legacy.EndReplay(replay);

        _rows = await _harness.ReadStoredRows();
        _formats = await _harness.ReadKeyFormats();
        _migrationRequired = await ((ICanReportKeyFormatMigration)_harness.CreateSinkForTheSameReadModel()).RequiresKeyFormatMigration();
    }

    [Fact] void should_record_the_promoted_table_as_k1() => _formats[Table].ShouldEqual("k1");
    [Fact] void should_promote_both_keys_as_separate_rows() => _rows.Length.ShouldEqual(2);
    [Fact] void should_store_the_keys_in_the_current_format() => _rows.All(row => ((string)row["id"]!).StartsWith("~k1:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_no_longer_require_a_migration() => _migrationRequired.ShouldBeFalse();
}
