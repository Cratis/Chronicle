// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances.given;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances;

public class and_reading_the_container_fails : a_sql_read_model
{
    Exception _error;

    async Task Because()
    {
        _database.ReadModelTable(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ProjectedColumn>>())
            .Returns<Task<DbContextScope<ReadModelDbContext>>>(_ => throw new InvalidOperationException("database unavailable"));
        Subscribe();
        _error = await Catch.Exception(async () => await _initial.Task.WaitAsync(TimeSpan.FromSeconds(15)));
    }

    [Fact] void should_report_a_named_sink_error() => _error.ShouldBeOfExactType<FailedToObserveReadModelInstances>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain("test-read-model");
    [Fact] void should_name_the_container() => _error.Message.ShouldContain("observed_read_models");
    [Fact] void should_name_the_sink() => _error.Message.ShouldEqual("Sink 'SQL' failed to observe read model 'test-read-model' in container 'observed_read_models'.");
    [Fact] void should_keep_the_underlying_error() => _error.InnerException.ShouldBeOfExactType<InvalidOperationException>();

    protected override DbConnection CreateConnection() => new SqliteConnection("DataSource=:memory:");
    protected override void Configure(DbContextOptionsBuilder<ReadModelDbContext> options, DbConnection connection) => options.UseSqlite(connection);
}
