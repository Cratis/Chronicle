// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances;

public class and_a_write_outside_the_page_changes_the_total_count : a_sql_read_model
{
    long _totalCount;

    async Task Because()
    {
        await Write(_firstId, "first");
        Subscribe(take: 1);
        _firstPage = await _initial.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await Write(_secondId, "outside");
        _secondPage = await _updated.Task.WaitAsync(TimeSpan.FromSeconds(15));
        _totalCount = (await _sink.GetInstances(take: 1)).TotalCount;
    }

    [Fact] void should_emit_an_unchanged_page() => NameOf(_secondPage).ShouldEqual(NameOf(_firstPage));
    [Fact] void should_allow_the_subscriber_to_refresh_the_total_count() => _totalCount.ShouldEqual(2L);

    readonly string _databaseName = Guid.NewGuid().ToString("N");

    protected override DbConnection CreateConnection() => new SqliteConnection($"Data Source={_databaseName};Mode=Memory;Cache=Shared");
    protected override void Configure(DbContextOptionsBuilder<ReadModelDbContext> options, DbConnection connection) => options.UseSqlite(connection.ConnectionString);
}
