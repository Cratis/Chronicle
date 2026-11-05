// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(SqlServerCollection.Name)]
public class when_opening_a_table_with_unreviewed_variants_on_sql_server(SqlServerFixture fixture) : given.a_sql_server_sequence_database(fixture)
{
    int _afterFirstAccess;
    int _afterSecondAccess;

    async Task Establish()
    {
        await CreateTable();
        await Seed(Table, "Alice", "All", "s1", 0);
        await Seed(Table, "alice", "All", "s1", 1);
        await using (var context = CreateContext())
        {
            await _migrator.CheckSpelling(context, Table);
        }

        StartMigrator();
    }

    async Task Because()
    {
        await using (var first = CreateContext())
        {
            await first.EnsureTableExists();
        }

        _afterFirstAccess = Warnings();
        await using (var second = CreateContext())
        {
            await second.EnsureTableExists();
        }

        _afterSecondAccess = Warnings();
    }

    int Warnings() => _log.Messages.Count(_ => _.Level == LogLevel.Warning);

    [Fact] void should_warn_on_the_first_access() => _afterFirstAccess.ShouldEqual(1);
    [Fact] void should_not_warn_again_on_the_second_access() => _afterSecondAccess.ShouldEqual(1);
}
