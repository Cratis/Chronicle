// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(SqlServerCollection.Name)]
public class when_upgrading_fitting_legacy_keys_on_sql_server(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    List<int> _beforeBytes;
    List<int> _afterBytes;
    List<string> _before;
    List<string> _after;
    List<string> _flags;
    List<string> _history;
    List<string> _primaryKey;
    Exception _sizeError;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _harness = new(_connectionString);
        await _harness.PrepareWideKeys(159);
        _beforeBytes = await _harness.KeyBytes();
        _before = await _harness.Rows();
    }

    async Task Because()
    {
        await _harness.Migrate();
        _afterBytes = await _harness.KeyBytes(upgraded: true);
        _sizeError = await Catch.Exception(() => _harness.Execute("UPDATE ClosedStreams SET StreamId = REPLICATE(N'p', 160)"));
        _after = await _harness.Rows();
        _flags = await _harness.Flags();
        _history = await _harness.History();
        _primaryKey = await _harness.PrimaryKeyColumns();
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_start_at_the_largest_key_size_that_leaves_room_for_the_hash() => _beforeBytes.ShouldEqual([868, 868]);
    [Fact] void should_accept_keys_of_exactly_900_bytes_after_upgrade() => _afterBytes.ShouldEqual([900, 900]);
    [Fact] void should_reject_a_legacy_key_of_870_bytes_after_upgrade() => ((SqlException)_sizeError).Number.ShouldEqual(1946);
    [Fact] void should_report_the_attempted_902_byte_key() => _sizeError.Message.ShouldContain("902");
    [Fact] void should_leave_all_stored_values_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_keep_the_existing_entries_pending_review() => _flags.ShouldEqual(["0", "0"]);
    [Fact] void should_record_the_migration() => _history.ShouldContain("NS-ClosedStreams-v20_0_2");
    [Fact] void should_include_the_exact_key_in_the_primary_key() => _primaryKey.ShouldContain("ExactKey");
}
