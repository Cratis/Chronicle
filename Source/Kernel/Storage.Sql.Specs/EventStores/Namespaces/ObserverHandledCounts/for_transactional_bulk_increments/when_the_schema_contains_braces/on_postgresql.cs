// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_the_schema_contains_braces;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_counts_storage(new("postgresql", () => DatabaseInSchema(fixture), fixture.DropDatabase))
{
    ObserverHandledCountEntry[] _rows;
    string _schema;

    async Task Establish()
    {
        await _harness.Storage.Increment("observer", "existing", Counts(3));
        _harness.Reset();
    }

    async Task Because()
    {
        await _harness.Storage.Increment("observer", [new((Key)"existing", Counts(4)), new((Key)"new", Counts(2))]);
        _rows = await _harness.Rows();
        await using var context = _harness.Context();
        _schema = await context.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
    }

    [Fact] void should_use_the_schema_with_literal_braces() => _schema.ShouldEqual("tenant{blue}");
    [Fact] void should_increment_existing_rows() => _rows.Single(_ => _.Partition == "existing").Count.ShouldEqual(7L);
    [Fact] void should_insert_new_rows() => _rows.Single(_ => _.Partition == "new").Count.ShouldEqual(2L);
    [Fact] void should_use_one_parameterized_statement() => _harness.Commands.Statements.ShouldEqual(1);

    static async Task<string> DatabaseInSchema(PostgreSqlFixture fixture)
    {
        var connectionString = await fixture.CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE SCHEMA \"tenant{blue}\"", connection);
        await command.ExecuteNonQueryAsync();
        return new NpgsqlConnectionStringBuilder(connectionString) { SearchPath = "\"tenant{blue}\"" }.ToString();
    }
}
