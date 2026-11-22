// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStringCache;

public class when_preserving_provider_normalization : given.a_cache
{
    bool[] _postgresql;
    bool[] _sqlServer;
    bool[] _sqlite;

    void Because()
    {
        _postgresql = NormalizeAll("Host=pg.example;Database=chronicle;Username=app;Password=\"semi;colon\";Minimum Pool Size=6;Maximum Pool Size=3", Configured);
        _sqlServer = NormalizeAll("Server=sql.example;Database=chronicle;User ID=app;Password=\"semi;colon\";Encrypt=true", "Server=other.example;Database=other;User ID=rotated;Password=secret;Encrypt=true");
        _sqlite = NormalizeAll("Data Source=chronicle.db;Pooling=false", "Data Source=other.db;Pooling=false");
    }

    [Fact] void should_preserve_postgresql_output_for_every_schema_placement_and_pooling_combination() => _postgresql.All(matches => matches).ShouldBeTrue();
    [Fact] void should_preserve_sql_server_output_for_every_schema_placement_and_pooling_combination() => _sqlServer.All(matches => matches).ShouldBeTrue();
    [Fact] void should_preserve_sqlite_output_for_every_schema_placement_and_pooling_combination() => _sqlite.All(matches => matches).ShouldBeTrue();

    bool[] NormalizeAll(string configured, string alternate)
    {
        string[] schemas = [string.Empty, "schema\"with_quote"];
        PostgreSqlPooling[] poolings = [new(), new() { MaxPoolSize = 0, ConnectionIdleLifetimeSeconds = -1, ConnectionPruningIntervalSeconds = 0 }, new() { MaxPoolSize = 20, ConnectionIdleLifetimeSeconds = 3, ConnectionPruningIntervalSeconds = 10 }];
        ResolvedConnection[] placements = [ResolvedConnection.Unchanged, new(Database: "tenant"), new(alternate), new(alternate, "tenant")];
        return (from schema in schemas
                from pooling in poolings
                from placement in placements
                let input = SqlConnectionStringInput.From(configured, schema, pooling, placement)
                let expected = SqlConnectionStrings.WithPooling(SqlConnectionStrings.WithSchema(SqlConnectionStrings.WithPlacement(configured, placement), schema), pooling)
                select Normalize(input) == expected).ToArray();
    }
}
