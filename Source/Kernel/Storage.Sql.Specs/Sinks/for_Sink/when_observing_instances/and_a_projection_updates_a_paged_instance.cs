// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances;

public class and_a_projection_updates_a_paged_instance : a_sql_read_model
{
    async Task Because() => await ObservePagedWrite();

    [Fact] void should_emit_the_initial_page() => NameOf(_firstPage).ShouldEqual("before");
    [Fact] void should_emit_a_fresh_page_after_the_write() => NameOf(_secondPage).ShouldEqual("after");

    readonly string _databaseName = Guid.NewGuid().ToString("N");

    protected override DbConnection CreateConnection() => new SqliteConnection($"Data Source={_databaseName};Mode=Memory;Cache=Shared");
    protected override void Configure(DbContextOptionsBuilder<ReadModelDbContext> options, DbConnection connection) => options.UseSqlite(connection.ConnectionString);
}
