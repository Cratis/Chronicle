// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceInventory.when_running_against_postgresql;

[Collection(PostgreSqlCollection.Name)]
public class when_names_differ_only_in_case(PostgreSqlFixture fixture) : Specification
{
    readonly string _name = $"case_{Guid.NewGuid():N}";
    string _connectionString;
    SqlDatabaseCatalog _catalog;
    bool _lowerExists;
    bool _upperExists;
    IReadOnlyDictionary<string, string> _groups;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _catalog = new SqlDatabaseCatalog(_connectionString);
        await Execute($"CREATE DATABASE \"{_name}\"");
    }

    async Task Because()
    {
        _lowerExists = await _catalog.Exists(_name);
        _upperExists = await _catalog.Exists(_name.ToUpperInvariant());
        _groups = await _catalog.Groups([_name, _name.ToUpperInvariant()]);
    }

    async Task Destroy()
    {
        await Execute($"DROP DATABASE IF EXISTS \"{_name}\" WITH (FORCE)");
        await fixture.DropDatabase(_connectionString);
    }

    [Fact] void should_find_the_database_under_its_own_spelling() => _lowerExists.ShouldBeTrue();
    [Fact] void should_not_find_it_under_another_case() => _upperExists.ShouldBeFalse();
    [Fact] void should_keep_the_spellings_distinct() => _groups[_name].ShouldNotEqual(_groups[_name.ToUpperInvariant()]);

    async Task Execute(string sql)
    {
        var maintenance = new NpgsqlConnectionStringBuilder(_connectionString) { Database = "postgres", Pooling = false }.ConnectionString;
        await using var connection = new NpgsqlConnection(maintenance);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Only generated database names from this fixture reach this helper.
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
