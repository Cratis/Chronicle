// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances;

public class and_reading_the_container_fails_once : a_sql_read_model
{
    int _reads;

    async Task Because()
    {
        await Write(_firstId, "first");
        _database.When(_ => _.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>(), Arg.Any<ConnectionPurpose>()))
            .Do(_ =>
            {
                if (Interlocked.Increment(ref _reads) == 1)
                {
                    throw new InvalidOperationException("database unavailable");
                }
            });
        Subscribe();
        _firstPage = await _initial.Task.WaitAsync(TimeSpan.FromSeconds(15), TimeProvider.System);
    }

    [Fact] void should_emit_the_page_once_the_container_can_be_read() => NameOf(_firstPage).ShouldEqual("first");
    [Fact] void should_have_retried_the_read() => (_reads >= 2).ShouldBeTrue();

    protected override DbConnection CreateConnection() => new SqliteConnection("DataSource=:memory:");
    protected override void Configure(DbContextOptionsBuilder<ReadModelDbContext> options, DbConnection connection) => options.UseSqlite(connection);
}
