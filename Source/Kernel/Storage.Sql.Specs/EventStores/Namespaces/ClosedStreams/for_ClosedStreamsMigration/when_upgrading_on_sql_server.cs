// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(SqlServerCollection.Name)]
public class when_upgrading_on_sql_server(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    List<string> _before;
    List<string> _after;
    List<string> _flags;
    int _rows;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _harness = new(_connectionString);
        await _harness.PrepareEarlierLayout("Alice", "bob ");
        _before = await _harness.Spellings();
    }

    async Task Because()
    {
        await _harness.Apply(true);
        _after = await _harness.Spellings();
        _flags = await _harness.Flags();
        await _harness.Insert("alice", ", \"ExactSpelling\"", ", 1");
        await _harness.Insert("Alice ", ", \"ExactSpelling\"", ", 1");
        _rows = (await _harness.Strings("SELECT CAST(COUNT(*) AS varchar(10)) AS \"Value\" FROM \"ClosedStreams\"")).Count == 1 ? int.Parse((await _harness.Strings("SELECT CAST(COUNT(*) AS varchar(10)) AS \"Value\" FROM \"ClosedStreams\""))[0]) : -1;
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_keep_every_row() => _after.Count.ShouldEqual(2);
    [Fact] void should_keep_every_stored_value_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_mark_old_rows_as_not_reviewed() => _flags.ShouldEqual(["0", "0"]);
    [Fact] void should_let_rows_that_differ_only_by_case_or_a_trailing_space_coexist() => _rows.ShouldEqual(4);
}
