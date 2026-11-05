// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(SqlServerCollection.Name)]
public class when_checking_spelling_on_sql_server(SqlServerFixture fixture) : given.a_sql_server_sequence_database(fixture)
{
    long[] _counts;
    IReadOnlyList<(LogLevel Level, string Message)> _messages;

    async Task Establish()
    {
        await CreateTable();
        await Seed(Table, "Alice", "All", "s1", 0);
        await Seed(Table, "alice", "All", "s1", 1);
        await Seed(Table, "Alice ", "All", "s1", 2);
        await Seed(Table, "Bob", "All", "s1", 3);
        await Seed(Table, "Carol", "All", "s2", 4);
        await Seed(Table, "Carol", "All", "S2", 5);
    }

    async Task Because()
    {
        await using var context = CreateContext();
        await _migrator.CheckSpelling(context, Table);
        _counts = await ReviewCounts(Table);
        _messages = _log.Messages;
    }

    [Fact] void should_count_one_event_source_id_in_more_than_one_spelling() => _counts[0].ShouldEqual(1L);
    [Fact] void should_count_one_event_stream_in_more_than_one_spelling() => _counts[1].ShouldEqual(1L);
    [Fact] void should_log_one_warning() => _messages.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_not_log_any_identifier_value() => _messages.Any(_ => new[] { "Alice", "alice", "Bob", "Carol", "s1", "s2", "S2" }.Any(value => _.Message.Contains(value, StringComparison.Ordinal))).ShouldBeFalse();
}
