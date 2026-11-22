// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_changing_normalization_inputs : given.a_cache
{
    NpgsqlConnectionStringBuilder _schema;
    NpgsqlConnectionStringBuilder _database;
    NpgsqlConnectionStringBuilder _host;
    NpgsqlConnectionStringBuilder _maximum;
    NpgsqlConnectionStringBuilder _idle;
    NpgsqlConnectionStringBuilder _pruning;

    void Establish() => Normalize(_input);

    void Because()
    {
        _schema = new(Normalize(_input with { Schema = "other" }));
        _database = new(Normalize(_input with { Database = "other" }));
        _host = new(Normalize(_input with { PlacementConnectionString = "Host=other.example;Database=chronicle;Username=app;Password=initial" }));
        _maximum = new(Normalize(_input with { MaxPoolSize = 42 }));
        _idle = new(Normalize(_input with { IdleLifetime = 60 }));
        _pruning = new(Normalize(_input with { PruningInterval = 10 }));
    }

    [Fact] void should_apply_the_changed_schema() => _schema.SearchPath.ShouldEqual("other");
    [Fact] void should_apply_the_changed_database() => _database.Database.ShouldEqual("other");
    [Fact] void should_apply_the_changed_resolved_host() => _host.Host.ShouldEqual("other.example");
    [Fact] void should_apply_the_changed_pool_maximum() => _maximum.MaxPoolSize.ShouldEqual(42);
    [Fact] void should_apply_the_changed_idle_lifetime() => _idle.ConnectionIdleLifetime.ShouldEqual(60);
    [Fact] void should_apply_the_changed_pruning_interval() => _pruning.ConnectionPruningInterval.ShouldEqual(10);
}
