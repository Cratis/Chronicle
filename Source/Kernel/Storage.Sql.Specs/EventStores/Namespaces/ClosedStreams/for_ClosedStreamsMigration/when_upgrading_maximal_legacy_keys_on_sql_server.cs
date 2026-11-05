// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(SqlServerCollection.Name)]
public class when_upgrading_maximal_legacy_keys_on_sql_server(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    Exception _sizeError;
    Exception _migrationError;
    List<int> _keyBytes;
    List<string> _before;
    List<string> _after;
    List<string> _historyBefore;
    List<string> _historyAfter;
    List<string> _primaryKeyBefore;
    List<string> _primaryKeyAfter;
    List<string> _columns;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _harness = new(_connectionString);
        await _harness.PrepareWideKeys(175);
        _keyBytes = await _harness.KeyBytes();
        _before = await _harness.Rows();
        _historyBefore = await _harness.History();
        _primaryKeyBefore = await _harness.PrimaryKeyColumns();
        _sizeError = await Catch.Exception(() => _harness.Execute("UPDATE ClosedStreams SET StreamId = REPLICATE(N'p', 176)"));
    }

    async Task Because()
    {
        _migrationError = await Catch.Exception(_harness.Migrate);
        _after = await _harness.Rows();
        _historyAfter = await _harness.History();
        _primaryKeyAfter = await _harness.PrimaryKeyColumns();
        _columns = await _harness.Strings("SELECT name AS Value FROM sys.columns WHERE object_id = OBJECT_ID(N'ClosedStreams')");
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_accept_legacy_keys_of_900_bytes() => _keyBytes.ShouldEqual([900, 900]);
    [Fact] void should_reject_the_next_larger_legacy_key() => ((SqlException)_sizeError).Number.ShouldEqual(1946);
    [Fact] void should_report_the_attempted_902_byte_key() => _sizeError.Message.ShouldContain("902");
    [Fact] void should_report_the_900_byte_limit() => _sizeError.Message.ShouldContain("900");
    [Fact] void should_refuse_the_migration_with_the_preflight_error() => ((SqlException)_migrationError).Number.ShouldEqual(50000);
    [Fact] void should_report_only_the_count_and_operator_guidance() => _migrationError.Message.ShouldEqual("2 closed stream entries have keys too long for the new primary key. Nothing changed. Shorten or remove those entries before upgrading.");
    [Fact] void should_leave_all_stored_rows_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_leave_migration_history_unchanged() => _historyAfter.ShouldEqual(_historyBefore);
    [Fact] void should_keep_the_existing_primary_key() => _primaryKeyAfter.ShouldEqual(_primaryKeyBefore);
    [Fact] void should_keep_all_seven_legacy_key_columns() => _primaryKeyAfter.Count.ShouldEqual(7);
    [Fact] void should_not_add_the_review_column() => _columns.ShouldNotContain("ExactSpelling");
    [Fact] void should_not_add_the_exact_key_column() => _columns.ShouldNotContain("ExactKey");
}
