// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(SqlServerCollection.Name)]
public class when_downgrading_with_spelling_variants_on_sql_server(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    given.UpgradeHarness _harness;
    Exception _error;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _harness = new(_connectionString);
        await _harness.PrepareEarlierLayout("Alice");
        await _harness.Apply(true);
        await _harness.Insert("alice", ", \"ExactSpelling\"", ", 1");
    }

    async Task Because() => _error = await Catch.Exception(() => _harness.Apply(false));

    Task Destroy() => fixture.DropDatabase(_connectionString);

    [Fact] void should_refuse_the_downgrade() => _error.ShouldNotBeNull();
    [Fact] void should_explain_that_entries_differ_only_by_spelling() => _error.Message.ShouldContain("differ only by letter case");
}
