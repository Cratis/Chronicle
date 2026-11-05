// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_on_postgresql(PostgreSqlFixture fixture) : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    List<string> _before;
    List<string> _after;
    List<string> _flags;

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
    }

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_keep_every_row() => _after.Count.ShouldEqual(2);
    [Fact] void should_keep_every_stored_value_unchanged() => _after.ShouldEqual(_before);
    [Fact] void should_mark_old_rows_as_exact() => _flags.ShouldEqual(["1", "1"]);
}
