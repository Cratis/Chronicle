// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(SqlServerCollection.Name)]
public class when_opening_a_table_without_a_review_row_on_sql_server(SqlServerFixture fixture) : given.a_sql_server_sequence_database(fixture)
{
    int _information;
    int _warnings;

    async Task Establish()
    {
        await CreateTable("other-sequence");
        await CreateTable();
        await using (var context = CreateContext("other-sequence"))
        {
            await _migrator.CheckSpelling(context, "other-sequence");
        }

        StartMigrator();
    }

    async Task Because()
    {
        await using var first = CreateContext();
        await first.EnsureTableExists();
        await using var second = CreateContext();
        await second.EnsureTableExists();
        _information = _log.Messages.Count(_ => _.Level == LogLevel.Information && _.Message.Contains("migrate-only", StringComparison.Ordinal));
        _warnings = _log.Messages.Count(_ => _.Level == LogLevel.Warning);
    }

    [Fact] void should_log_one_information_message() => _information.ShouldEqual(1);
    [Fact] void should_not_warn() => _warnings.ShouldEqual(0);
}
